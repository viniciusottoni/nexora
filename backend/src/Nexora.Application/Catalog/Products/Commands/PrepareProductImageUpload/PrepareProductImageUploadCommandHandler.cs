using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Abstractions.Storage;
using Nexora.Contracts.Catalog;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Catalog.Products.Commands.PrepareProductImageUpload;

internal sealed class PrepareProductImageUploadCommandHandler
    : IRequestHandler<PrepareProductImageUploadCommand, Result<PrepareProductImageUploadResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly IProductMediaStorage _storage;

    public PrepareProductImageUploadCommandHandler(
        IApplicationDbContext db,
        ICurrentTenantContext tenantContext,
        IProductMediaStorage storage)
    {
        _db = db;
        _tenantContext = tenantContext;
        _storage = storage;
    }

    public async Task<Result<PrepareProductImageUploadResponse>> Handle(
        PrepareProductImageUploadCommand request,
        CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result<PrepareProductImageUploadResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado ao seu usuário.",
                ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;

        var productExists = await _db.Products
            .AsNoTracking()
            .AnyAsync(p => p.Id == request.ProductId && p.TenantId == tenantId && p.DeletedAt == null, cancellationToken);

        if (!productExists)
        {
            return Result<PrepareProductImageUploadResponse>.Failure("Produto não encontrado.", ApiErrorCodes.ProductNotFound);
        }

        ProductMediaUpload upload;
        try
        {
            upload = await _storage.CreateUploadAsync(
                new ProductMediaUploadRequest(tenantId, request.ProductId, request.ContentType, request.Bytes, request.Sha256),
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return Result<PrepareProductImageUploadResponse>.Failure(ex.Message, ApiErrorCodes.ProductMediaStorageUnavailable);
        }

        return Result<PrepareProductImageUploadResponse>.Success(
            new PrepareProductImageUploadResponse(upload.UploadUrl, upload.PublicUrl, upload.ExpiresAt));
    }
}
