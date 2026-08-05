using System.Text.Json;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Cashier.Support;
using Nexora.Contracts.Cashier;
using Nexora.Domain.Cashier;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Cashier.Commands.RegisterCashMovement;

/// <summary>Cenários Gherkin "Sangria registrada", "Suprimento de troco", "Sangria acima do limite" e "Movimento sem caixa aberto" (US-056 §4).</summary>
internal sealed class RegisterCashMovementCommandHandler : IRequestHandler<RegisterCashMovementCommand, Result<RegisterCashMovementResponse>>
{
    /// <summary>US-056 §5 (RN-011): ação sensível catalogada em <c>SensitiveActionCatalog</c> para sangria acima do limite.</summary>
    public const string WithdrawalAboveLimitAction = "WITHDRAWAL_ABOVE_LIMIT";

    private readonly IApplicationDbContext _db;
    private readonly IEventOriginProvider _eventOrigin;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly IAuthorizationTokenValidator _authorizationValidator;

    public RegisterCashMovementCommandHandler(
        IApplicationDbContext db,
        IEventOriginProvider eventOrigin,
        ICurrentTenantContext tenantContext,
        IAuthorizationTokenValidator authorizationValidator)
    {
        _db = db;
        _eventOrigin = eventOrigin;
        _tenantContext = tenantContext;
        _authorizationValidator = authorizationValidator;
    }

    public async Task<Result<RegisterCashMovementResponse>> Handle(RegisterCashMovementCommand request, CancellationToken cancellationToken)
    {
        var storeId = _tenantContext.StoreId!.Value;
        var operatorId = _tenantContext.UserId!.Value;

        var session = await _db.CashSessions
            .Where(s => s.StoreId == storeId && s.OperatorId == operatorId && s.Status == CashSessionStatus.Open)
            .OrderByDescending(s => s.OpenedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (session is null)
        {
            return Result<RegisterCashMovementResponse>.Failure("Não há caixa aberto para registrar o movimento.", ApiErrorCodes.NoOpenCashSession);
        }

        var type = request.Type == "SUPPLY" ? CashMovementType.Supply : CashMovementType.Withdrawal;

        Guid? authorizedBy = null;
        if (type == CashMovementType.Withdrawal)
        {
            var tenantConfig = await _db.TenantConfigs.AsNoTracking()
                .SingleOrDefaultAsync(c => c.TenantId == session.TenantId, cancellationToken);
            var maxWithoutAuth = CashPolicy.ResolveMaxWithdrawalWithoutAuth(tenantConfig?.Operation);

            if (request.Amount > maxWithoutAuth)
            {
                var grant = await _authorizationValidator.ValidateAsync(request.AuthorizationToken, WithdrawalAboveLimitAction, cancellationToken);
                if (grant.IsFailure)
                {
                    return Result<RegisterCashMovementResponse>.Failure(grant.Error!, grant.Code, grant.Errors);
                }

                authorizedBy = grant.Value!.AuthorizedBy;
            }
        }

        // Composição ANTES de registrar — o movimento ainda não foi persistido (SaveChangesAsync só
        // roda ao final do TransactionBehavior), então uma nova consulta ao banco não o enxergaria;
        // soma/subtrai o valor diretamente sobre a composição já lida.
        var expectedBefore = await CashExpectedAmountCalculator.CalculateAsync(_db, session.Id, session.OpeningAmount, cancellationToken);
        var newExpected = expectedBefore.Total + (type == CashMovementType.Supply ? request.Amount : -request.Amount);

        var now = DateTimeOffset.UtcNow;
        var movement = session.RegisterMovement(type, request.Amount, request.Reason, operatorId, now, authorizedBy);

        // EVT-031 cash.movement.registered (US-056 §6): type, amount, reason, authorizedBy.
        _db.DomainEvents.Add(DomainEvent.Create(
            session.TenantId,
            type: "cash.movement.registered",
            aggregateType: "cash_movement",
            aggregateId: movement.Id,
            payload: JsonSerializer.Serialize(new { type = request.Type, amount = request.Amount, reason = request.Reason, authorizedBy }),
            origin: _eventOrigin.Origin,
            occurredAt: now,
            storeId: session.StoreId,
            actorId: operatorId,
            authorizedBy: authorizedBy,
            deviceId: _tenantContext.DeviceId));

        return Result<RegisterCashMovementResponse>.Success(
            new RegisterCashMovementResponse(CashSessionMapper.Map(movement), newExpected));
    }
}
