using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Abstractions.Storage;
using Nexora.Contracts.Catalog;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Catalog.Products.Commands.ConfirmProductImage;

internal sealed class ConfirmProductImageCommandHandler
    : IRequestHandler<ConfirmProductImageCommand, Result<ProductImageResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly IProductMediaStorage _mediaStorage;

    public ConfirmProductImageCommandHandler(
        IApplicationDbContext db,
        ICurrentTenantContext tenantContext,
        IProductMediaStorage mediaStorage)
    {
        _db = db;
        _tenantContext = tenantContext;
        _mediaStorage = mediaStorage;
    }

    public async Task<Result<ProductImageResponse>> Handle(ConfirmProductImageCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result<ProductImageResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado ao seu usuário.",
                ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;

        var product = await _db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProductId && p.TenantId == tenantId && p.DeletedAt == null, cancellationToken);

        if (product is null)
        {
            return Result<ProductImageResponse>.Failure("Produto não encontrado.", ApiErrorCodes.ProductNotFound);
        }

        var uploadRequest = new ProductMediaUploadRequest(
            tenantId,
            product.Id,
            request.ContentType,
            request.Bytes,
            request.Sha256);
        if (!_mediaStorage.IsExpectedPublicUrl(uploadRequest, request.Url))
        {
            return Result<ProductImageResponse>.Failure(
                "A foto confirmada não corresponde ao upload preparado para este produto.",
                ApiErrorCodes.ProductMediaNotFound);
        }

        var asset = MediaAsset.Create(
            tenantId,
            ownerType: "PRODUCT",
            variant: "PHOTO",
            url: request.Url,
            contentHash: request.Sha256.ToLowerInvariant(),
            ownerId: product.Id,
            width: request.Width,
            height: request.Height,
            bytes: request.Bytes,
            mimeType: request.ContentType);

        _db.MediaAssets.Add(asset);

        var now = DateTimeOffset.UtcNow;

        // EVT-050 product.updated — a foto é um campo do produto para efeitos de propagação ao
        // edge/cardápio público, mesmo não sendo uma coluna de Product (ver docstring de
        // ConfirmProductImageCommand).
        _db.DomainEvents.Add(DomainEvent.Create(
            tenantId,
            type: "product.updated",
            aggregateType: "product",
            aggregateId: product.Id,
            payload: System.Text.Json.JsonSerializer.Serialize(new { productId = product.Id, changedKeys = new[] { "imageUrl" } }),
            origin: "CLOUD",
            occurredAt: now,
            actorId: _tenantContext.UserId,
            deviceId: _tenantContext.DeviceId));

        // SaveChangesAsync é feito pelo TransactionBehavior.

        return Result<ProductImageResponse>.Success(new ProductImageResponse(asset.Id, asset.Url));
    }
}
