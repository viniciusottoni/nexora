using System.Text.Json;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Application.Abstractions.Security;
using Nexora.Contracts.Catalog;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Catalog.Availability.Commands.MarkProductAvailable;

internal sealed class MarkProductAvailableCommandHandler
    : IRequestHandler<MarkProductAvailableCommand, Result<ProductAvailabilityResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly IEventOriginProvider _eventOrigin;
    private readonly IAvailabilityBroadcaster _broadcaster;

    public MarkProductAvailableCommandHandler(
        IApplicationDbContext db,
        ICurrentTenantContext tenantContext,
        IEventOriginProvider eventOrigin,
        IAvailabilityBroadcaster broadcaster)
    {
        _db = db;
        _tenantContext = tenantContext;
        _eventOrigin = eventOrigin;
        _broadcaster = broadcaster;
    }

    public async Task<Result<ProductAvailabilityResponse>> Handle(
        MarkProductAvailableCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null || _tenantContext.UserId is null)
        {
            return Result<ProductAvailabilityResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado ao seu usuário.",
                ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;
        var actorId = _tenantContext.UserId.Value;
        var now = DateTimeOffset.UtcNow;

        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Id == request.ProductId && p.TenantId == tenantId && p.DeletedAt == null, cancellationToken);

        if (product is null)
        {
            // Ver nota equivalente em MarkProductUnavailableCommandHandler sobre o uso do literal
            // "PRODUCT_NOT_FOUND" em vez do símbolo ApiErrorCodes.ProductNotFound (US-010) — ausente
            // deste worktree isolado.
            return Result<ProductAvailabilityResponse>.Failure("Produto não encontrado.", "PRODUCT_NOT_FOUND");
        }

        if (!product.IsAvailable)
        {
            var previousReason = product.UnavailableReason;
            product.MarkAvailable();

            _db.AuditLogs.Add(AuditLog.Create(
                tenantId,
                action: "PRODUCT_MARKED_AVAILABLE",
                entity: "product",
                occurredAt: now,
                storeId: _tenantContext.StoreId,
                actorId: actorId,
                deviceId: _tenantContext.DeviceId,
                entityId: product.Id,
                before: JsonSerializer.Serialize(new { isAvailable = false, reason = previousReason }),
                after: JsonSerializer.Serialize(new { isAvailable = true })));

            // EVT-051 product.availability_changed (US-015 §6).
            _db.DomainEvents.Add(DomainEvent.Create(
                tenantId,
                type: "product.availability_changed",
                aggregateType: "product",
                aggregateId: product.Id,
                payload: JsonSerializer.Serialize(new { productId = product.Id, isAvailable = true }),
                origin: _eventOrigin.Origin,
                occurredAt: now,
                storeId: _tenantContext.StoreId,
                actorId: actorId,
                deviceId: _tenantContext.DeviceId));

            // Broadcast síncrono, aguardado dentro do Handle (ver IAvailabilityBroadcaster).
            await _broadcaster.ProductMarkedAvailableAsync(tenantId, product.Id, cancellationToken);
        }

        return Result<ProductAvailabilityResponse>.Success(new ProductAvailabilityResponse(
            product.Id, product.Name, product.IsAvailable, product.UnavailableReason, product.UnavailableSince));
    }
}
