using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Abstractions.Storage;
using Nexora.Contracts.Branding;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;

namespace Nexora.Application.Branding.Commands.PrepareBrandingUpload;

internal sealed class PrepareBrandingUploadCommandHandler
    : IRequestHandler<PrepareBrandingUploadCommand, Result<UploadBrandingAssetResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly IBrandingStorage _storage;

    public PrepareBrandingUploadCommandHandler(
        IApplicationDbContext db,
        ICurrentTenantContext tenantContext,
        IBrandingStorage storage)
    {
        _db = db;
        _tenantContext = tenantContext;
        _storage = storage;
    }

    public async Task<Result<UploadBrandingAssetResponse>> Handle(
        PrepareBrandingUploadCommand request,
        CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result<UploadBrandingAssetResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado ao seu usuário.",
                ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;

        BrandingUpload upload;
        try
        {
            upload = await _storage.CreateUploadAsync(
                new BrandingUploadRequest(tenantId, request.Kind, request.ContentType, request.Bytes, request.Sha256),
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return Result<UploadBrandingAssetResponse>.Failure(ex.Message, ApiErrorCodes.BrandingStorageUnavailable);
        }

        var asset = MediaAsset.Create(
            tenantId,
            ownerType: "BRANDING",
            variant: request.Kind,
            url: upload.PublicUrl,
            contentHash: request.Sha256.ToLowerInvariant(),
            ownerId: tenantId,
            bytes: request.Bytes,
            mimeType: request.ContentType);

        _db.MediaAssets.Add(asset);

        // SaveChangesAsync é feito pelo TransactionBehavior.

        return Result<UploadBrandingAssetResponse>.Success(
            new UploadBrandingAssetResponse(asset.Id, upload.UploadUrl, upload.PublicUrl, upload.ExpiresAt));
    }
}
