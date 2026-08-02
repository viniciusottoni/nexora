using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Contracts.Catalog;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Catalog.Variants.Queries.GetVariant;

internal sealed class GetVariantQueryHandler : IRequestHandler<GetVariantQuery, Result<VariantResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public GetVariantQueryHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<VariantResponse>> Handle(GetVariantQuery request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result<VariantResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado ao seu usuário.",
                ApiErrorCodes.TenantContextMissing);
        }

        if (!ChannelParser.TryParse(request.Channel, out var channel))
        {
            return Result<VariantResponse>.Failure("Canal de venda inválido.", ApiErrorCodes.PriceChannelInvalid);
        }

        var tenantId = _tenantContext.TenantId.Value;

        var variant = await _db.ProductVariants
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == request.VariantId && v.TenantId == tenantId && v.DeletedAt == null, cancellationToken);

        if (variant is null)
        {
            return Result<VariantResponse>.Failure("Variante não encontrada.", ApiErrorCodes.VariantNotFound);
        }

        var currentPrice = await _db.Prices
            .AsNoTracking()
            .Where(p => p.VariantId == variant.Id && p.Channel == channel && p.ValidTo == null)
            .OrderByDescending(p => p.ValidFrom)
            .FirstOrDefaultAsync(cancellationToken);

        return Result<VariantResponse>.Success(new VariantResponse(
            variant.Id,
            variant.ProductId,
            variant.Name,
            variant.Sku,
            variant.SizeCode,
            variant.PrepMinutes,
            variant.IsDefault,
            variant.IsActive,
            currentPrice?.Amount,
            currentPrice is null ? null : channel.ToString()));
    }
}
