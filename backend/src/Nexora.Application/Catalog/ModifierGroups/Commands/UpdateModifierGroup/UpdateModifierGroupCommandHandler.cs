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

namespace Nexora.Application.Catalog.ModifierGroups.Commands.UpdateModifierGroup;

internal sealed class UpdateModifierGroupCommandHandler
    : IRequestHandler<UpdateModifierGroupCommand, Result<ModifierGroupResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly IEventOriginProvider _eventOrigin;
    private readonly ILogger<UpdateModifierGroupCommandHandler> _logger;

    public UpdateModifierGroupCommandHandler(
        IApplicationDbContext db,
        ICurrentTenantContext tenantContext,
        IEventOriginProvider eventOrigin,
        ILogger<UpdateModifierGroupCommandHandler> logger)
    {
        _db = db;
        _tenantContext = tenantContext;
        _eventOrigin = eventOrigin;
        _logger = logger;
    }

    public async Task<Result<ModifierGroupResponse>> Handle(UpdateModifierGroupCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result<ModifierGroupResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado à requisição.",
                ApiErrorCodes.TenantContextMissing);
        }

        if (!PermissionAuthorization.HasPermission(_tenantContext.Permissions, "catalog:write"))
        {
            return Result<ModifierGroupResponse>.Failure(
                "Seu perfil não tem permissão para alterar o cardápio.",
                ApiErrorCodes.AuthPermissionDenied);
        }

        var tenantId = _tenantContext.TenantId.Value;
        var now = DateTimeOffset.UtcNow;

        var group = await _db.ModifierGroups
            .FirstOrDefaultAsync(g => g.Id == request.GroupId && g.TenantId == tenantId && g.DeletedAt == null, cancellationToken);

        if (group is null)
        {
            return Result<ModifierGroupResponse>.Failure("Grupo de modificadores não encontrado.", ApiErrorCodes.ModifierGroupNotFound);
        }

        if (group.IsRequired && request.MinSelect < 1)
        {
            return Result<ModifierGroupResponse>.Failure(
                "Um grupo obrigatório precisa exigir ao menos uma seleção.",
                ApiErrorCodes.ValidationError,
                new Dictionary<string, string[]>
                {
                    [nameof(request.MinSelect)] = ["Informe ao menos uma seleção para o grupo obrigatório."],
                });
        }

        group.UpdateSelectionRange(request.MinSelect, request.MaxSelect);

        var linkedProductIds = await _db.ProductModifierGroups
            .Where(pg => pg.GroupId == group.Id && pg.TenantId == tenantId)
            .Select(pg => pg.ProductId)
            .ToListAsync(cancellationToken);

        // RN "grupo reusado em N produtos": a mudança de regra de seleção já vale para todos os
        // produtos vinculados (normalização via FK, nenhuma cópia por produto) — o evento abaixo
        // só avisa quem consome o cardápio (cache local/edge) que precisa buscar de novo.
        foreach (var productId in linkedProductIds)
        {
            _db.DomainEvents.Add(DomainEvent.Create(
                tenantId: tenantId,
                type: "product.updated",
                aggregateType: "product",
                aggregateId: productId,
                payload: JsonSerializer.Serialize(new { productId, modifierGroupId = group.Id }),
                origin: _eventOrigin.Origin,
                occurredAt: now,
                actorId: _tenantContext.UserId,
                deviceId: _tenantContext.DeviceId));
        }

        _db.AuditLogs.Add(AuditLog.Create(
            tenantId: tenantId,
            action: "MODIFIER_GROUP_UPDATED",
            entity: "modifier_group",
            occurredAt: now,
            actorId: _tenantContext.UserId,
            deviceId: _tenantContext.DeviceId,
            entityId: group.Id,
            after: JsonSerializer.Serialize(new { group.MinSelect, group.MaxSelect })));

        var modifiers = await _db.Modifiers
            .Where(m => m.GroupId == group.Id && m.TenantId == tenantId && m.DeletedAt == null)
            .OrderBy(m => m.SortOrder)
            .Select(m => new ModifierResponse(m.Id, m.GroupId, m.Name, m.PriceDelta, m.IngredientId, m.Quantity, m.IsAvailable, m.SortOrder))
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Grupo de modificadores atualizado. TenantId={TenantId}, GroupId={GroupId}, MinSelect={MinSelect}, MaxSelect={MaxSelect}",
            tenantId, group.Id, group.MinSelect, group.MaxSelect);

        return Result<ModifierGroupResponse>.Success(new ModifierGroupResponse(
            group.Id, group.Name, group.MinSelect, group.MaxSelect, group.IsRequired, group.SortOrder, modifiers, linkedProductIds));
    }
}
