using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Contracts.Catalog;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Catalog.Variants.Queries.ListVariantsForProduct;

internal sealed class ListVariantsForProductQueryHandler : IRequestHandler<ListVariantsForProductQuery, Result<VariantListResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public ListVariantsForProductQueryHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<VariantListResponse>> Handle(ListVariantsForProductQuery request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result<VariantListResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado ao seu usuário.",
                ApiErrorCodes.TenantContextMissing);
        }

        if (!ChannelParser.TryParse(request.Channel, out var channel))
        {
            return Result<VariantListResponse>.Failure("Canal de venda inválido.", ApiErrorCodes.PriceChannelInvalid);
        }

        var tenantId = _tenantContext.TenantId.Value;

        var product = await _db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProductId && p.TenantId == tenantId && p.DeletedAt == null, cancellationToken);

        if (product is null)
        {
            return Result<VariantListResponse>.Failure("Produto não encontrado.", ApiErrorCodes.ProductNotFound);
        }

        var channelName = channel.ToString();

        var items = await _db.ProductVariants
            .AsNoTracking()
            .Where(v => v.ProductId == product.Id && v.TenantId == tenantId && v.DeletedAt == null)
            .OrderByDescending(v => v.IsDefault)
            .ThenBy(v => v.Name)
            .Select(v => new VariantResponse(
                v.Id,
                v.ProductId,
                v.Name,
                v.Sku,
                v.SizeCode,
                v.PrepMinutes,
                v.IsDefault,
                v.IsActive,
                _db.Prices
                    .Where(p => p.VariantId == v.Id && p.Channel == channel && p.ValidTo == null)
                    .Select(p => (decimal?)p.Amount)
                    .FirstOrDefault(),
                _db.Prices
                    .Where(p => p.VariantId == v.Id && p.Channel == channel && p.ValidTo == null)
                    .Any()
                    ? channelName
                    : null))
            .ToListAsync(cancellationToken);

        return Result<VariantListResponse>.Success(new VariantListResponse(items));
    }
}
