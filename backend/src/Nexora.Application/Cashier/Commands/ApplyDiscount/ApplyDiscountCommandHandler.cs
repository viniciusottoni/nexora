using System.Text.Json;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Cashier.Support;
using Nexora.Application.Tables.Sessions;
using Nexora.Contracts.Cashier;
using Nexora.Domain.Operation;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Cashier.Commands.ApplyDiscount;

/// <summary>
/// US-054 §4 — ver docstring de <see cref="ApplyDiscountCommand"/>. A ordem é: (1) resolver a base
/// (subtotal da sessão ou total bruto do item) e converter percentual↔valor, (2) avaliar o limite
/// configurado, (3) SE acima, exigir e validar <c>X-Authorization-Token</c> (ADR-023), (4) só então
/// persistir — nunca aplica o desconto antes de confirmar a autorização.
/// </summary>
internal sealed class ApplyDiscountCommandHandler : IRequestHandler<ApplyDiscountCommand, Result<ApplyDiscountResponse>>
{
    private const string AuthorizationAction = "DISCOUNT_ABOVE_LIMIT";

    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly IEventOriginProvider _eventOrigin;
    private readonly IAuthorizationTokenValidator _authorizationValidator;

    public ApplyDiscountCommandHandler(
        IApplicationDbContext db,
        ICurrentTenantContext tenantContext,
        IEventOriginProvider eventOrigin,
        IAuthorizationTokenValidator authorizationValidator)
    {
        _db = db;
        _tenantContext = tenantContext;
        _eventOrigin = eventOrigin;
        _authorizationValidator = authorizationValidator;
    }

    public async Task<Result<ApplyDiscountResponse>> Handle(ApplyDiscountCommand request, CancellationToken cancellationToken)
    {
        var session = await _db.TableSessions.SingleOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken);
        if (session is null)
        {
            return Result<ApplyDiscountResponse>.Failure("Sessão não encontrada.", ApiErrorCodes.TableSessionNotFound);
        }

        if (session.Status is TableSessionStatus.Paid or TableSessionStatus.Closed)
        {
            return Result<ApplyDiscountResponse>.Failure(
                "Esta comanda já foi paga ou encerrada.", ApiErrorCodes.TableSessionNotOpen);
        }

        var scope = request.Scope.Trim().ToUpperInvariant();
        var actorId = _tenantContext.UserId!.Value;
        var occurredAt = DateTimeOffset.UtcNow;

        OrderItem? item = null;
        decimal baseAmount;

        if (scope == "ITEM")
        {
            item = await _db.OrderItems
                .Where(i => i.Id == request.OrderItemId && _db.Orders.Any(o => o.Id == i.OrderId && o.SessionId == session.Id))
                .SingleOrDefaultAsync(cancellationToken);

            if (item is null || item.Status == OrderItemStatus.Cancelled)
            {
                return Result<ApplyDiscountResponse>.Failure(
                    "Item não encontrado nesta comanda.", ApiErrorCodes.DiscountItemNotFound);
            }

            baseAmount = (item.UnitPrice * item.Quantity) + item.ModifiersTotal;
        }
        else
        {
            var currentBill = await BillQueryCoordinator.BuildAsync(_db, session, "SINGLE", null, null, null, cancellationToken);
            if (currentBill.IsFailure)
            {
                return Result<ApplyDiscountResponse>.Failure(currentBill.Error!, currentBill.Code);
            }

            baseAmount = currentBill.Value!.Subtotal;
        }

        if (baseAmount <= 0)
        {
            return Result<ApplyDiscountResponse>.Failure(
                "Não é possível aplicar desconto sobre um valor zerado.", ApiErrorCodes.DiscountInvalidAmount);
        }

        decimal percent;
        decimal amount;
        if (request.Percent is { } requestedPercent)
        {
            percent = requestedPercent;
            amount = Math.Round(baseAmount * percent / 100m, 2, MidpointRounding.AwayFromZero);
        }
        else if (request.Amount is { } requestedAmount)
        {
            if (requestedAmount > baseAmount)
            {
                return Result<ApplyDiscountResponse>.Failure(
                    "O desconto não pode ser maior que o valor da conta.", ApiErrorCodes.DiscountInvalidAmount);
            }

            amount = requestedAmount;
            percent = Math.Round(amount / baseAmount * 100m, 2, MidpointRounding.AwayFromZero);
        }
        else
        {
            return Result<ApplyDiscountResponse>.Failure(
                "Informe o percentual ou o valor do desconto.", ApiErrorCodes.DiscountInvalidAmount);
        }

        var tenantConfig = await _db.TenantConfigs.AsNoTracking().SingleOrDefaultAsync(c => c.TenantId == session.TenantId, cancellationToken);
        var limitPercent = DiscountPolicy.ResolveMaxWithoutAuthPercent(tenantConfig?.Operation);

        Guid? authorizedBy = null;
        if (percent > limitPercent)
        {
            var grant = await _authorizationValidator.ValidateAsync(request.AuthorizationToken, AuthorizationAction, cancellationToken);
            if (grant.IsFailure)
            {
                return Result<ApplyDiscountResponse>.Failure(
                    grant.Error!,
                    grant.Code,
                    new Dictionary<string, string[]>
                    {
                        ["action"] = new[] { "APPLY_DISCOUNT" },
                        ["limitPercent"] = new[] { limitPercent.ToString(System.Globalization.CultureInfo.InvariantCulture) },
                        ["requestedPercent"] = new[] { percent.ToString(System.Globalization.CultureInfo.InvariantCulture) },
                    });
            }

            authorizedBy = grant.Value!.AuthorizedBy;
        }

        if (scope == "ITEM")
        {
            item!.ApplyDiscount(amount, request.Reason, actorId, authorizedBy);
        }
        else
        {
            session.ApplyDiscount(percent, request.Reason, actorId, authorizedBy);
        }

        _db.DomainEvents.Add(DomainEvent.Create(
            session.TenantId,
            type: "discount.applied",
            aggregateType: scope == "ITEM" ? "order_item" : "table_session",
            aggregateId: scope == "ITEM" ? item!.Id : session.Id,
            payload: JsonSerializer.Serialize(new { amount, percent, reason = request.Reason, authorizedBy, scope }),
            origin: _eventOrigin.Origin,
            occurredAt: occurredAt,
            storeId: session.StoreId,
            actorId: actorId,
            authorizedBy: authorizedBy));

        if (authorizedBy is not null)
        {
            _db.DomainEvents.Add(DomainEvent.Create(
                session.TenantId,
                type: "authorization.granted",
                aggregateType: "table_session",
                aggregateId: session.Id,
                payload: JsonSerializer.Serialize(new { action = "APPLY_DISCOUNT", authorizedBy }),
                origin: _eventOrigin.Origin,
                occurredAt: occurredAt,
                storeId: session.StoreId,
                actorId: actorId,
                authorizedBy: authorizedBy));
        }

        _db.AuditLogs.Add(AuditLog.Create(
            session.TenantId,
            action: "DISCOUNT_APPLIED",
            entity: scope == "ITEM" ? "order_item" : "table_session",
            occurredAt: occurredAt,
            storeId: session.StoreId,
            actorId: actorId,
            authorizedBy: authorizedBy,
            entityId: scope == "ITEM" ? item!.Id : session.Id,
            after: JsonSerializer.Serialize(new { amount, percent, scope }),
            reason: request.Reason));

        var finalBill = await BillQueryCoordinator.BuildAsync(_db, session, "SINGLE", null, null, null, cancellationToken);
        var finalTotal = finalBill.IsSuccess ? finalBill.Value!.Total : session.TotalAmount;
        // Desconto por ITEM já está embutido no subtotal (OrderItem.TotalPrice líquido) — o campo
        // `Discount` da resposta reporta o valor deste desconto especificamente aplicado agora,
        // não o `BillResponse.Discount` (que só mede o desconto de SESSÃO, RN-011 escopo distinto).
        var finalDiscount = scope == "ITEM" ? amount : (finalBill.IsSuccess ? finalBill.Value!.Discount : session.DiscountAmount);

        DiscountAuthorizerResponse? authorizerResponse = null;
        if (authorizedBy is { } authorizedByValue)
        {
            var authorizerName = await _db.Users.AsNoTracking()
                .Where(u => u.Id == authorizedByValue)
                .Select(u => u.Name)
                .SingleOrDefaultAsync(cancellationToken);
            authorizerResponse = new DiscountAuthorizerResponse(authorizedByValue, authorizerName ?? string.Empty);
        }

        var response = new ApplyDiscountResponse(
            new DiscountedSessionResponse(finalDiscount, session.DiscountPercent, finalTotal),
            authorizerResponse);

        return Result<ApplyDiscountResponse>.Success(response);
    }
}
