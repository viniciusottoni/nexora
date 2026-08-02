using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Contracts.Catalog;
using Nexora.Domain.Catalog;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;

namespace Nexora.Application.Catalog.Categories.Commands.CreateCategory;

internal sealed class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Result<CategoryResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public CreateCategoryCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<CategoryResponse>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
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

        var category = Category.Create(tenantId, request.Name, request.Description, request.Position);

        _db.Categories.Add(category);

        _db.AuditLogs.Add(AuditLog.Create(
            tenantId,
            action: "CATEGORY_CREATED",
            entity: "category",
            occurredAt: now,
            actorId: actorId,
            deviceId: _tenantContext.DeviceId,
            entityId: category.Id,
            after: JsonSerializer.Serialize(new { name = category.Name, description = category.Description, position = category.SortOrder })));

        // Categoria é configuração de cardápio (mesmo padrão de tenant.config_updated usado por
        // Stations/Branding) — não é o EVT-050 (product.created/updated), que é específico de produto.
        _db.DomainEvents.Add(DomainEvent.Create(
            tenantId,
            type: "tenant.config_updated",
            aggregateType: "category",
            aggregateId: category.Id,
            payload: JsonSerializer.Serialize(new { categoryId = category.Id, changedKeys = new[] { "created" } }),
            origin: "CLOUD",
            occurredAt: now,
            actorId: actorId,
            deviceId: _tenantContext.DeviceId));

        // SaveChangesAsync é feito pelo TransactionBehavior.

        return Result<CategoryResponse>.Success(new CategoryResponse(
            category.Id,
            category.Name,
            category.Description,
            category.SortOrder,
            category.IsActive,
            ProductCount: 0));
    }
}
