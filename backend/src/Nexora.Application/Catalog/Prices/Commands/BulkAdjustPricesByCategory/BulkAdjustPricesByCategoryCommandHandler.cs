using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Catalog.Prices.Queries.ListVariantPricesByChannel;
using Nexora.Contracts.Catalog;
using Nexora.Domain.Catalog;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Catalog.Prices.Commands.BulkAdjustPricesByCategory;

internal sealed class BulkAdjustPricesByCategoryCommandHandler
    : IRequestHandler<BulkAdjustPricesByCategoryCommand, Result<BulkAdjustPricesResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public BulkAdjustPricesByCategoryCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<BulkAdjustPricesResponse>> Handle(BulkAdjustPricesByCategoryCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null || _tenantContext.UserId is null)
        {
            return Result<BulkAdjustPricesResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado ao seu usuário.",
                ApiErrorCodes.TenantContextMissing);
        }

        if (!PricingChannelParser.TryParse(request.Channel, out var channel))
        {
            return Result<BulkAdjustPricesResponse>.Failure("Canal de venda inválido.", ApiErrorCodes.PriceTableChannelInvalid);
        }

        var tenantId = _tenantContext.TenantId.Value;
        var actorId = _tenantContext.UserId.Value;
        var now = DateTimeOffset.UtcNow;

        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId && c.TenantId == tenantId && c.DeletedAt == null, cancellationToken);

        if (category is null)
        {
            return Result<BulkAdjustPricesResponse>.Failure("Categoria não encontrada.", ApiErrorCodes.PriceTableCategoryNotFound);
        }

        // Todas as variações ATIVAS de produtos ATIVOS da categoria (US-014 §3.1, "reajuste em
        // massa por categoria"). Produto/variante inativos ficam de fora — reajustar um item que
        // não está à venda não tem efeito observável e evita mexer em preço "morto".
        var variants = await _db.ProductVariants
            .Where(v => v.TenantId == tenantId
                && v.DeletedAt == null
                && v.IsActive
                && v.Product.CategoryId == category.Id
                && v.Product.DeletedAt == null
                && v.Product.IsActive)
            .ToListAsync(cancellationToken);

        if (variants.Count == 0)
        {
            // Categoria sem variação ativa nenhuma: nada para ajustar, mas não é erro — mesmo
            // espírito idempotente do restante do módulo (ex.: SetVariantPriceCommandHandler
            // trata "nada mudou" como sucesso, não como falha).
            return Result<BulkAdjustPricesResponse>.Success(new BulkAdjustPricesResponse(Updated: 0, EffectiveFrom: now));
        }

        var variantIds = variants.Select(v => v.Id).ToList();

        var pricesByVariant = (await _db.Prices
                .AsNoTracking()
                .Where(p => variantIds.Contains(p.VariantId) && p.ValidTo == null)
                .ToListAsync(cancellationToken))
            .GroupBy(p => p.VariantId)
            .ToDictionary(g => g.Key, g => (IReadOnlyCollection<Price>)g.ToList());

        // Passo 1 (dry-run): resolve o preço efetivo de cada variação e calcula o novo valor SEM
        // tocar o DbContext. Se qualquer variação resultaria em preço negativo, a chamada inteira
        // é recusada aqui — nada é adicionado ao contexto, então não há nenhuma mutação parcial
        // para desfazer (US-014 §12, "reajuste em massa é transacional; falha parcial não deixa
        // preços inconsistentes").
        var plan = new List<(ProductVariant Variant, ResolvedChannelPrice Effective, decimal NewAmount)>();
        foreach (var variant in variants)
        {
            var currentPrices = pricesByVariant.TryGetValue(variant.Id, out var list) ? list : Array.Empty<Price>();
            var effective = ChannelPriceResolver.Resolve(channel, currentPrices);

            if (effective.Amount is null)
            {
                // Variação sem preço algum (nem próprio, nem base DineIn) — nada a reajustar.
                continue;
            }

            var newAmount = BulkPriceAdjustmentCalculator.Apply(effective.Amount.Value, request.Percent);

            if (newAmount < 0)
            {
                return Result<BulkAdjustPricesResponse>.Failure(
                    "O reajuste resultaria em preço negativo para ao menos um item da categoria.",
                    ApiErrorCodes.PriceBulkAdjustNegativeResult);
            }

            plan.Add((variant, effective, newAmount));
        }

        // Passo 2: aplica de fato — só agora o DbContext é mutado.
        var updated = 0;
        foreach (var (variant, effective, newAmount) in plan)
        {
            if (newAmount == effective.Amount)
            {
                // Reajuste sem efeito prático (ex.: percent = 0, ou arredondamento devolveu o
                // mesmo valor) — no-op, não historiza nem emite evento.
                continue;
            }

            Price? ownCurrent = null;
            if (!effective.IsInherited && effective.SourcePriceId is not null)
            {
                ownCurrent = pricesByVariant.TryGetValue(variant.Id, out var list)
                    ? list.FirstOrDefault(p => p.Id == effective.SourcePriceId)
                    : null;
            }

            if (ownCurrent is not null)
            {
                await _db.Prices
                    .Where(p => p.Id == ownCurrent.Id && p.ValidTo == null)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.ValidTo, now), cancellationToken);
            }

            var newPrice = Price.Create(tenantId, variant.Id, channel, newAmount, actorId, now);
            _db.Prices.Add(newPrice);

            // EVT-052 price.changed (US-014 §6). Criado antes do AuditLog para correlacionar via
            // DomainEventId (E-09/US-090).
            var priceChangedEvent = DomainEvent.Create(
                tenantId,
                type: "price.changed",
                aggregateType: "product_variant",
                aggregateId: variant.Id,
                payload: JsonSerializer.Serialize(new
                {
                    variantId = variant.Id,
                    channel = channel.ToString(),
                    oldAmount = effective.Amount,
                    newAmount = newPrice.Amount,
                    validFrom = newPrice.ValidFrom
                }),
                origin: "CLOUD",
                occurredAt: now,
                actorId: actorId,
                deviceId: _tenantContext.DeviceId);
            _db.DomainEvents.Add(priceChangedEvent);

            _db.AuditLogs.Add(AuditLog.Create(
                tenantId,
                action: "PRICE_CHANGED",
                entity: "price",
                occurredAt: now,
                actorId: actorId,
                deviceId: _tenantContext.DeviceId,
                entityId: newPrice.Id,
                before: JsonSerializer.Serialize(new { amount = effective.Amount, channel = channel.ToString(), inherited = effective.IsInherited }),
                after: JsonSerializer.Serialize(new { amount = newPrice.Amount, channel = channel.ToString() }),
                domainEventId: priceChangedEvent.Id));

            updated++;
        }

        if (updated > 0)
        {
            // Registro-resumo do reajuste em massa (US-014 §11, "Histórico de reajustes por
            // período e por autor") — além das linhas individuais por preço acima, mais fácil de
            // consultar por categoria/canal/percentual do que somar N linhas de "price".
            _db.AuditLogs.Add(AuditLog.Create(
                tenantId,
                action: "PRICE_BULK_ADJUSTED",
                entity: "category",
                occurredAt: now,
                actorId: actorId,
                deviceId: _tenantContext.DeviceId,
                entityId: category.Id,
                after: JsonSerializer.Serialize(new { categoryId = category.Id, channel = channel.ToString(), percent = request.Percent, updated })));
        }

        // SaveChangesAsync é feito pelo TransactionBehavior.

        return Result<BulkAdjustPricesResponse>.Success(new BulkAdjustPricesResponse(updated, now));
    }
}
