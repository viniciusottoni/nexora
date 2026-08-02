using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Contracts.Catalog;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Catalog.Categories.Commands.UpdateCategory;

internal sealed class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, Result<CategoryResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public UpdateCategoryCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<CategoryResponse>> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null || _tenantContext.UserId is null)
        {
            return Result<CategoryResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado ao seu usuário.",
                ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;
        var actorId = _tenantContext.UserId.Value;
        var now = DateTimeOffset.UtcNow;

        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId && c.TenantId == tenantId && c.DeletedAt == null, cancellationToken);

        if (category is null)
        {
            return Result<CategoryResponse>.Failure("Categoria não encontrada.", ApiErrorCodes.CategoryNotFound);
        }

        var before = JsonSerializer.Serialize(new { name = category.Name, description = category.Description, position = category.SortOrder, isActive = category.IsActive });

        var changedKeys = new List<string>();
        if (request.Name is not null && request.Name != category.Name) changedKeys.Add("name");
        if (request.Description is not null && request.Description != category.Description) changedKeys.Add("description");
        if (request.Position is not null && request.Position != category.SortOrder) changedKeys.Add("position");

        category.UpdateDetails(
            request.Name ?? category.Name,
            request.Description ?? category.Description,
            request.Position ?? category.SortOrder);

        if (request.IsActive == true && !category.IsActive)
        {
            changedKeys.Add("isActive");
            category.Activate();
        }
        else if (request.IsActive == false && category.IsActive)
        {
            changedKeys.Add("isActive");
            category.Deactivate();
        }

        if (changedKeys.Count > 0)
        {
            var after = JsonSerializer.Serialize(new { name = category.Name, description = category.Description, position = category.SortOrder, isActive = category.IsActive });

            _db.AuditLogs.Add(AuditLog.Create(
                tenantId,
                action: "CATEGORY_UPDATED",
                entity: "category",
                occurredAt: now,
                actorId: actorId,
                deviceId: _tenantContext.DeviceId,
                entityId: category.Id,
                before: before,
                after: after));

            _db.DomainEvents.Add(DomainEvent.Create(
                tenantId,
                type: "tenant.config_updated",
                aggregateType: "category",
                aggregateId: category.Id,
                payload: JsonSerializer.Serialize(new { categoryId = category.Id, changedKeys }),
                origin: "CLOUD",
                occurredAt: now,
                actorId: actorId,
                deviceId: _tenantContext.DeviceId));
        }

        var productCount = await _db.Products
            .CountAsync(p => p.CategoryId == category.Id && p.DeletedAt == null, cancellationToken);

        // SaveChangesAsync é feito pelo TransactionBehavior.

        return Result<CategoryResponse>.Success(new CategoryResponse(
            category.Id,
            category.Name,
            category.Description,
            category.SortOrder,
            category.IsActive,
            productCount));
    }
}
