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

namespace Nexora.Application.Catalog.ProductModifierGroups.Commands.LinkModifierGroupToProduct;

internal sealed class LinkModifierGroupToProductCommandHandler
    : IRequestHandler<LinkModifierGroupToProductCommand, Result<ProductModifierGroupResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly IEventOriginProvider _eventOrigin;
    private readonly ILogger<LinkModifierGroupToProductCommandHandler> _logger;

    public LinkModifierGroupToProductCommandHandler(
        IApplicationDbContext db,
        ICurrentTenantContext tenantContext,
        IEventOriginProvider eventOrigin,
        ILogger<LinkModifierGroupToProductCommandHandler> logger)
    {
        _db = db;
        _tenantContext = tenantContext;
        _eventOrigin = eventOrigin;
        _logger = logger;
    }

    public async Task<Result<ProductModifierGroupResponse>> Handle(
        LinkModifierGroupToProductCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result<ProductModifierGroupResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado à requisição.",
                ApiErrorCodes.TenantContextMissing);
        }

        if (!PermissionAuthorization.HasPermission(_tenantContext.Permissions, "catalog:write"))
        {
            return Result<ProductModifierGroupResponse>.Failure(
                "Seu perfil não tem permissão para alterar o cardápio.", ApiErrorCodes.AuthPermissionDenied);
        }

        var tenantId = _tenantContext.TenantId.Value;
        var now = DateTimeOffset.UtcNow;

        var productExists = await _db.Products
            .AsNoTracking()
            .AnyAsync(p => p.Id == request.ProductId && p.TenantId == tenantId && p.DeletedAt == null, cancellationToken);

        if (!productExists)
        {
            return Result<ProductModifierGroupResponse>.Failure("Produto não encontrado.", ApiErrorCodes.ModifierGroupProductNotFound);
        }

        var groupExists = await _db.ModifierGroups
            .AsNoTracking()
            .AnyAsync(g => g.Id == request.GroupId && g.TenantId == tenantId && g.DeletedAt == null, cancellationToken);

        if (!groupExists)
        {
            return Result<ProductModifierGroupResponse>.Failure("Grupo de modificadores não encontrado.", ApiErrorCodes.ModifierGroupNotFound);
        }

        var alreadyLinked = await _db.ProductModifierGroups
            .AsNoTracking()
            .AnyAsync(pg => pg.ProductId == request.ProductId && pg.GroupId == request.GroupId && pg.TenantId == tenantId, cancellationToken);

        if (alreadyLinked)
        {
            return Result<ProductModifierGroupResponse>.Failure(
                "Este grupo já está vinculado a este produto.", ApiErrorCodes.ProductModifierGroupAlreadyLinked);
        }

        var link = ProductModifierGroup.Create(tenantId, request.ProductId, request.GroupId, request.SortOrder);
        _db.ProductModifierGroups.Add(link);

        _db.DomainEvents.Add(DomainEvent.Create(
            tenantId: tenantId,
            type: "product.updated",
            aggregateType: "product",
            aggregateId: request.ProductId,
            payload: JsonSerializer.Serialize(new { productId = request.ProductId, modifierGroupId = request.GroupId, linked = true }),
            origin: _eventOrigin.Origin,
            occurredAt: now,
            actorId: _tenantContext.UserId,
            deviceId: _tenantContext.DeviceId));

        _db.AuditLogs.Add(AuditLog.Create(
            tenantId: tenantId,
            action: "PRODUCT_MODIFIER_GROUP_LINKED",
            entity: "product_modifier_group",
            occurredAt: now,
            actorId: _tenantContext.UserId,
            deviceId: _tenantContext.DeviceId,
            entityId: request.ProductId,
            after: JsonSerializer.Serialize(new { request.ProductId, request.GroupId, request.SortOrder })));

        _logger.LogInformation(
            "Grupo de modificadores vinculado a produto. TenantId={TenantId}, ProductId={ProductId}, GroupId={GroupId}",
            tenantId, request.ProductId, request.GroupId);

        return Result<ProductModifierGroupResponse>.Success(
            new ProductModifierGroupResponse(request.ProductId, request.GroupId, request.SortOrder));
    }
}
