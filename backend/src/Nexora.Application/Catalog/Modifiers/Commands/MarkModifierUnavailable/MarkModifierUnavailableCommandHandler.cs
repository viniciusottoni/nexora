using System.Text.Json;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Contracts.Catalog;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using Nexora.Shared.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Nexora.Application.Catalog.Modifiers.Commands.MarkModifierUnavailable;

internal sealed class MarkModifierUnavailableCommandHandler : IRequestHandler<MarkModifierUnavailableCommand, Result<ModifierResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly IEventOriginProvider _eventOrigin;
    private readonly ILogger<MarkModifierUnavailableCommandHandler> _logger;

    public MarkModifierUnavailableCommandHandler(
        IApplicationDbContext db,
        ICurrentTenantContext tenantContext,
        IEventOriginProvider eventOrigin,
        ILogger<MarkModifierUnavailableCommandHandler> logger)
    {
        _db = db;
        _tenantContext = tenantContext;
        _eventOrigin = eventOrigin;
        _logger = logger;
    }

    public async Task<Result<ModifierResponse>> Handle(MarkModifierUnavailableCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result<ModifierResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado à requisição.",
                ApiErrorCodes.TenantContextMissing);
        }

        if (!PermissionAuthorization.HasPermission(_tenantContext.Permissions, "catalog:set_unavailable")
            && !PermissionAuthorization.HasPermission(_tenantContext.Permissions, "catalog:write"))
        {
            return Result<ModifierResponse>.Failure("Seu perfil não tem permissão para alterar o cardápio.", ApiErrorCodes.AuthPermissionDenied);
        }

        var tenantId = _tenantContext.TenantId.Value;
        var now = DateTimeOffset.UtcNow;

        var modifier = await _db.Modifiers
            .FirstOrDefaultAsync(
                m => m.Id == request.ModifierId && m.GroupId == request.GroupId && m.TenantId == tenantId && m.DeletedAt == null,
                cancellationToken);

        if (modifier is null)
        {
            return Result<ModifierResponse>.Failure("Modificador não encontrado.", ApiErrorCodes.ModifierNotFound);
        }

        modifier.MarkUnavailable();

        var linkedProductIds = await _db.ProductModifierGroups
            .Where(pg => pg.GroupId == modifier.GroupId && pg.TenantId == tenantId)
            .Select(pg => pg.ProductId)
            .ToListAsync(cancellationToken);

        foreach (var productId in linkedProductIds)
        {
            _db.DomainEvents.Add(DomainEvent.Create(
                tenantId: tenantId,
                type: "product.updated",
                aggregateType: "product",
                aggregateId: productId,
                payload: JsonSerializer.Serialize(new { productId, modifierGroupId = modifier.GroupId, modifierId = modifier.Id, isAvailable = false }),
                origin: _eventOrigin.Origin,
                occurredAt: now,
                actorId: _tenantContext.UserId,
                deviceId: _tenantContext.DeviceId));
        }

        _db.AuditLogs.Add(AuditLog.Create(
            tenantId: tenantId,
            action: "MODIFIER_MARKED_UNAVAILABLE",
            entity: "modifier",
            occurredAt: now,
            actorId: _tenantContext.UserId,
            deviceId: _tenantContext.DeviceId,
            entityId: modifier.Id));

        _logger.LogInformation("Modificador marcado indisponível. TenantId={TenantId}, ModifierId={ModifierId}", tenantId, modifier.Id);

        return Result<ModifierResponse>.Success(new ModifierResponse(
            modifier.Id, modifier.GroupId, modifier.Name, modifier.PriceDelta, modifier.IngredientId,
            modifier.Quantity, modifier.IsAvailable, modifier.SortOrder));
    }
}
