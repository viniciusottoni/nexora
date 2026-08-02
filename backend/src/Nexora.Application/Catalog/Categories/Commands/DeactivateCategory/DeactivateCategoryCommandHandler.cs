using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Catalog.Categories.Commands.DeactivateCategory;

internal sealed class DeactivateCategoryCommandHandler : IRequestHandler<DeactivateCategoryCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public DeactivateCategoryCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result> Handle(DeactivateCategoryCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null || _tenantContext.UserId is null)
        {
            return Result.Failure(
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
            return Result.Failure("Categoria não encontrada.", ApiErrorCodes.CategoryNotFound);
        }

        if (!category.IsActive)
        {
            return Result.Success();
        }

        category.Deactivate();

        _db.AuditLogs.Add(AuditLog.Create(
            tenantId,
            action: "CATEGORY_DEACTIVATED",
            entity: "category",
            occurredAt: now,
            actorId: actorId,
            deviceId: _tenantContext.DeviceId,
            entityId: category.Id));

        _db.DomainEvents.Add(DomainEvent.Create(
            tenantId,
            type: "tenant.config_updated",
            aggregateType: "category",
            aggregateId: category.Id,
            payload: JsonSerializer.Serialize(new { categoryId = category.Id, changedKeys = new[] { "isActive" } }),
            origin: "CLOUD",
            occurredAt: now,
            actorId: actorId,
            deviceId: _tenantContext.DeviceId));

        // SaveChangesAsync é feito pelo TransactionBehavior.

        return Result.Success();
    }
}
