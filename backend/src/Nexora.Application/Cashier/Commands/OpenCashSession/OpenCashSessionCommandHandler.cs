using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Cashier.Support;
using Nexora.Application.Catalog.Availability;
using Nexora.Contracts.Cashier;
using Nexora.Domain.Cashier;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Cashier.Commands.OpenCashSession;

/// <summary>Cenários Gherkin "Abertura com fundo" e "Um caixa por operador e turno" (US-055 §4).</summary>
internal sealed class OpenCashSessionCommandHandler : IRequestHandler<OpenCashSessionCommand, Result<CashSessionResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IEventOriginProvider _eventOrigin;
    private readonly ICurrentTenantContext _tenantContext;

    public OpenCashSessionCommandHandler(IApplicationDbContext db, IEventOriginProvider eventOrigin, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _eventOrigin = eventOrigin;
        _tenantContext = tenantContext;
    }

    public async Task<Result<CashSessionResponse>> Handle(OpenCashSessionCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId!.Value;
        var storeId = _tenantContext.StoreId!.Value;
        var operatorId = _tenantContext.UserId!.Value;

        // RN "um caixa por operador e turno" (US-055 §4) — checagem de aplicação ANTES do insert;
        // uq_cash_open (store_id, operator_id, filtrado por status <> CLOSED) é o backstop de banco
        // contra a corrida de duas aberturas simultâneas (mesma dupla checagem de TableSessionOpener
        // para uq_session_open).
        var existing = await _db.CashSessions.AsNoTracking()
            .Where(s => s.StoreId == storeId && s.OperatorId == operatorId && s.Status != CashSessionStatus.Closed)
            .Select(s => new { s.Id })
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            return Result<CashSessionResponse>.Failure(
                "Já existe um caixa aberto para este operador neste turno.",
                ApiErrorCodes.CashSessionAlreadyOpen,
                new Dictionary<string, string[]> { ["sessionId"] = new[] { existing.Id.ToString() } });
        }

        var now = DateTimeOffset.UtcNow;
        var tenantConfig = await _db.TenantConfigs.AsNoTracking().SingleOrDefaultAsync(c => c.TenantId == tenantId, cancellationToken);
        var startHourUtc = BusinessDayPolicy.ResolveStartHourUtc(tenantConfig?.Operation);
        var businessDay = DateOnly.FromDateTime(BusinessDayPolicy.CurrentBusinessDayStart(now, startHourUtc).UtcDateTime);

        var session = CashSession.Create(
            tenantId, storeId, operatorId, businessDay, request.OpeningAmount, now, _tenantContext.DeviceId);

        _db.CashSessions.Add(session);

        // EVT-030 cash.session.opened (US-055 §6): operatorId, openingAmount.
        _db.DomainEvents.Add(DomainEvent.Create(
            tenantId,
            type: "cash.session.opened",
            aggregateType: "cash_session",
            aggregateId: session.Id,
            payload: System.Text.Json.JsonSerializer.Serialize(new { operatorId, openingAmount = request.OpeningAmount }),
            origin: _eventOrigin.Origin,
            occurredAt: now,
            storeId: storeId,
            actorId: operatorId,
            deviceId: _tenantContext.DeviceId));

        return Result<CashSessionResponse>.Success(CashSessionMapper.Map(session));
    }
}
