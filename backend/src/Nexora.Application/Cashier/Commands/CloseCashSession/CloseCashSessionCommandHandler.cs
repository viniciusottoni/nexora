using System.Text.Json;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Alerts.Support;
using Nexora.Application.Cashier.Support;
using Nexora.Contracts.Cashier;
using Nexora.Domain.Cashier;
using Nexora.Domain.Metrics;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Cashier.Commands.CloseCashSession;

/// <summary>
/// Cenários Gherkin "Divergência no fechamento", "Fechamento sem divergência" e "Mesa aberta no
/// fechamento" (US-055 §4). O alerta ao gestor (US-055 §6/§11, "gestor deve ser alertado") reaproveita
/// o motor de alertas do E-08 (<see cref="IAlertRaiser"/>, já roteado para <c>MANAGER</c> via
/// <c>AlertRoutingConfig[AlertTypes.CashDivergence]</c>) — a mesma varredura em lote da nuvem
/// (<c>EvaluateCloudAlertConditionsCommandHandler.EvaluateCashDivergenceAsync</c>) cobre o gestor
/// fora da loja; aqui o alerta é levantado imediatamente no edge (entrega local, US-055 §9) usando o
/// MESMO limiar de <see cref="CashPolicy.ResolveDivergenceJustificationThreshold"/> que já exige a
/// justificativa — o Gherkin descreve as duas consequências sob a mesma condição "acima do limiar".
/// </summary>
internal sealed class CloseCashSessionCommandHandler : IRequestHandler<CloseCashSessionCommand, Result<CloseCashSessionResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IEventOriginProvider _eventOrigin;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly IAuthorizationTokenValidator _authorizationValidator;
    private readonly IAlertRaiser _alertRaiser;

    public CloseCashSessionCommandHandler(
        IApplicationDbContext db,
        IEventOriginProvider eventOrigin,
        ICurrentTenantContext tenantContext,
        IAuthorizationTokenValidator authorizationValidator,
        IAlertRaiser alertRaiser)
    {
        _db = db;
        _eventOrigin = eventOrigin;
        _tenantContext = tenantContext;
        _authorizationValidator = authorizationValidator;
        _alertRaiser = alertRaiser;
    }

    public async Task<Result<CloseCashSessionResponse>> Handle(CloseCashSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _db.CashSessions.SingleOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken);
        if (session is null)
        {
            return Result<CloseCashSessionResponse>.Failure("Sessão de caixa não encontrada.", ApiErrorCodes.CashSessionNotFound);
        }

        if (session.Status == CashSessionStatus.Closed)
        {
            return Result<CloseCashSessionResponse>.Failure("Este caixa já foi fechado.", ApiErrorCodes.CashSessionAlreadyClosed);
        }

        var now = DateTimeOffset.UtcNow;

        // RN-018 (US-055 §5): mesa aberta bloqueia o fechamento, salvo autorização registrada.
        var guard = await CashCloseGuard.EnforceAsync(
            _db, _authorizationValidator, session.TenantId, session.StoreId, session.Id, request.AuthorizationToken, now, cancellationToken);
        if (guard.IsFailure)
        {
            return Result<CloseCashSessionResponse>.Failure(guard.Error!, guard.Code, guard.Errors);
        }

        var expected = await CashExpectedAmountCalculator.CalculateAsync(_db, session.Id, session.OpeningAmount, cancellationToken);
        session.SetExpectedAmount(expected.Total);

        var divergence = request.CountedAmount - expected.Total;
        var tenantConfig = await _db.TenantConfigs.AsNoTracking()
            .SingleOrDefaultAsync(c => c.TenantId == session.TenantId, cancellationToken);
        var threshold = CashPolicy.ResolveDivergenceJustificationThreshold(tenantConfig?.Operation);
        var requiresJustification = Math.Abs(divergence) > threshold;

        if (requiresJustification && string.IsNullOrWhiteSpace(request.Justification))
        {
            return Result<CloseCashSessionResponse>.Failure(
                "A divergência encontrada exige uma justificativa antes de fechar o caixa.", ApiErrorCodes.CashJustificationRequired);
        }

        session.Close(
            closedBy: _tenantContext.UserId!.Value,
            countedAmount: request.CountedAmount,
            closedAt: now,
            authorizedBy: guard.Value!.AuthorizedBy,
            justification: request.Justification);

        // EVT-036 cash.session.closed (US-055 §6): expected, counted, divergence.
        _db.DomainEvents.Add(DomainEvent.Create(
            session.TenantId,
            type: "cash.session.closed",
            aggregateType: "cash_session",
            aggregateId: session.Id,
            payload: JsonSerializer.Serialize(new { expected = expected.Total, counted = request.CountedAmount, divergence }),
            origin: _eventOrigin.Origin,
            occurredAt: now,
            storeId: session.StoreId,
            actorId: _tenantContext.UserId,
            authorizedBy: guard.Value.AuthorizedBy,
            deviceId: _tenantContext.DeviceId));

        if (requiresJustification)
        {
            await _alertRaiser.RaiseAsync(new RaiseAlertRequest(
                session.TenantId,
                session.StoreId,
                AlertTypes.CashDivergence,
                AlertSeverity.High,
                $"Sessão de caixa fechada com divergência de R$ {divergence:F2}.",
                "cash_session",
                session.Id), cancellationToken);
        }

        return Result<CloseCashSessionResponse>.Success(new CloseCashSessionResponse(
            expected.Total, request.CountedAmount, divergence, requiresJustification, CashSessionMapper.Map(session)));
    }
}
