using System.Text.Json;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Contracts.Catalog;
using Nexora.Domain.Catalog;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using Nexora.Shared.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Nexora.Application.Catalog.Modifiers.Commands.CreateModifier;

internal sealed class CreateModifierCommandHandler : IRequestHandler<CreateModifierCommand, Result<ModifierResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly IEventOriginProvider _eventOrigin;
    private readonly ILogger<CreateModifierCommandHandler> _logger;

    public CreateModifierCommandHandler(
        IApplicationDbContext db,
        ICurrentTenantContext tenantContext,
        IEventOriginProvider eventOrigin,
        ILogger<CreateModifierCommandHandler> logger)
    {
        _db = db;
        _tenantContext = tenantContext;
        _eventOrigin = eventOrigin;
        _logger = logger;
    }

    public async Task<Result<ModifierResponse>> Handle(CreateModifierCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result<ModifierResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado à requisição.",
                ApiErrorCodes.TenantContextMissing);
        }

        if (!PermissionAuthorization.HasPermission(_tenantContext.Permissions, "catalog:write"))
        {
            return Result<ModifierResponse>.Failure("Seu perfil não tem permissão para alterar o cardápio.", ApiErrorCodes.AuthPermissionDenied);
        }

        var tenantId = _tenantContext.TenantId.Value;
        var now = DateTimeOffset.UtcNow;

        var group = await _db.ModifierGroups
            .FirstOrDefaultAsync(g => g.Id == request.GroupId && g.TenantId == tenantId && g.DeletedAt == null, cancellationToken);

        if (group is null)
        {
            return Result<ModifierResponse>.Failure("Grupo de modificadores não encontrado.", ApiErrorCodes.ModifierGroupNotFound);
        }

        if (request.IngredientId is { } ingredientId)
        {
            var ingredientExists = await _db.Ingredients
                .AsNoTracking()
                .AnyAsync(i => i.Id == ingredientId && i.TenantId == tenantId && i.DeletedAt == null, cancellationToken);

            if (!ingredientExists)
            {
                return Result<ModifierResponse>.Failure("Insumo informado não encontrado.", ApiErrorCodes.ModifierIngredientNotFound);
            }
        }

        var modifier = Modifier.Create(
            tenantId,
            group.Id,
            request.Name.Trim(),
            request.PriceDelta,
            request.IngredientId,
            request.Quantity,
            request.SortOrder);

        _db.Modifiers.Add(modifier);

        var linkedProductIds = await _db.ProductModifierGroups
            .Where(pg => pg.GroupId == group.Id && pg.TenantId == tenantId)
            .Select(pg => pg.ProductId)
            .ToListAsync(cancellationToken);

        foreach (var productId in linkedProductIds)
        {
            _db.DomainEvents.Add(DomainEvent.Create(
                tenantId: tenantId,
                type: "product.updated",
                aggregateType: "product",
                aggregateId: productId,
                payload: JsonSerializer.Serialize(new { productId, modifierGroupId = group.Id, modifierId = modifier.Id }),
                origin: _eventOrigin.Origin,
                occurredAt: now,
                actorId: _tenantContext.UserId,
                deviceId: _tenantContext.DeviceId));
        }

        _db.AuditLogs.Add(AuditLog.Create(
            tenantId: tenantId,
            action: "MODIFIER_CREATED",
            entity: "modifier",
            occurredAt: now,
            actorId: _tenantContext.UserId,
            deviceId: _tenantContext.DeviceId,
            entityId: modifier.Id));

        _logger.LogInformation(
            "Modificador criado. TenantId={TenantId}, GroupId={GroupId}, ModifierId={ModifierId}, PriceDelta={PriceDelta}",
            tenantId, group.Id, modifier.Id, modifier.PriceDelta);

        return Result<ModifierResponse>.Success(new ModifierResponse(
            modifier.Id, modifier.GroupId, modifier.Name, modifier.PriceDelta, modifier.IngredientId,
            modifier.Quantity, modifier.IsAvailable, modifier.SortOrder));
    }
}
