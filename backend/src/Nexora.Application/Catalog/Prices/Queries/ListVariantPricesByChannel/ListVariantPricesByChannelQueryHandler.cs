using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Contracts.Catalog;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Catalog.Prices.Queries.ListVariantPricesByChannel;

internal sealed class ListVariantPricesByChannelQueryHandler
    : IRequestHandler<ListVariantPricesByChannelQuery, Result<VariantPriceTableResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public ListVariantPricesByChannelQueryHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<VariantPriceTableResponse>> Handle(ListVariantPricesByChannelQuery request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result<VariantPriceTableResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado ao seu usuário.",
                ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;

        var variant = await _db.ProductVariants
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == request.VariantId && v.TenantId == tenantId && v.DeletedAt == null, cancellationToken);

        if (variant is null)
        {
            return Result<VariantPriceTableResponse>.Failure("Variante não encontrada.", ApiErrorCodes.PriceTableVariantNotFound);
        }

        var currentPrices = await _db.Prices
            .AsNoTracking()
            .Where(p => p.VariantId == variant.Id && p.ValidTo == null)
            .ToListAsync(cancellationToken);

        var rows = ChannelPriceResolver.ResolveAll(currentPrices)
            .Select(resolved => new VariantChannelPriceRow(resolved.Channel.ToString(), resolved.Amount, resolved.IsInherited, resolved.ValidFrom))
            .ToList();

        return Result<VariantPriceTableResponse>.Success(new VariantPriceTableResponse(variant.Id, variant.ProductId, rows));
    }
}
