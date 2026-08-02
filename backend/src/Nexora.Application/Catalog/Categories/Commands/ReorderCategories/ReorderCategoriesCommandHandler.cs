using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Catalog.Categories.Commands.ReorderCategories;

internal sealed class ReorderCategoriesCommandHandler : IRequestHandler<ReorderCategoriesCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public ReorderCategoriesCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result> Handle(ReorderCategoriesCommand request, CancellationToken cancellationToken)
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

        var categories = await _db.Categories
            .Where(c => c.TenantId == tenantId && c.DeletedAt == null)
            .ToListAsync(cancellationToken);

        var existingIds = categories.Select(c => c.Id).ToHashSet();
        var requestedIds = request.Order.ToHashSet();

        // "A ordem deve ser respeitada no cardápio da mesa e do delivery" (US-010 §4) — só é seguro
        // reaplicar SortOrder quando o conjunto enviado bate exatamente com as categorias existentes
        // do tenant, nunca um subconjunto (que apagaria silenciosamente a posição das demais).
        if (!existingIds.SetEquals(requestedIds))
        {
            return Result.Failure(
                "A lista enviada não corresponde às categorias existentes.",
                ApiErrorCodes.CatalogReorderSetMismatch);
        }

        var byId = categories.ToDictionary(c => c.Id);
        var changed = new List<Guid>();

        for (var index = 0; index < request.Order.Count; index++)
        {
            var category = byId[request.Order[index]];
            var newPosition = (short)index;
            if (category.SortOrder == newPosition)
            {
                continue;
            }

            category.UpdateDetails(category.Name, category.Description, newPosition);
            changed.Add(category.Id);
        }

        if (changed.Count > 0)
        {
            _db.AuditLogs.Add(AuditLog.Create(
                tenantId,
                action: "CATEGORY_REORDERED",
                entity: "category",
                occurredAt: now,
                actorId: actorId,
                deviceId: _tenantContext.DeviceId,
                after: JsonSerializer.Serialize(new { order = request.Order })));

            foreach (var categoryId in changed)
            {
                _db.DomainEvents.Add(DomainEvent.Create(
                    tenantId,
                    type: "tenant.config_updated",
                    aggregateType: "category",
                    aggregateId: categoryId,
                    payload: JsonSerializer.Serialize(new { categoryId, changedKeys = new[] { "position" } }),
                    origin: "CLOUD",
                    occurredAt: now,
                    actorId: actorId,
                    deviceId: _tenantContext.DeviceId));
            }
        }

        // SaveChangesAsync é feito pelo TransactionBehavior.

        return Result.Success();
    }
}
