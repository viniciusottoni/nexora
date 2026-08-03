using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Catalog.FractionPricing;
using Nexora.Application.Orders.Commands.AddOrderItem;
using Nexora.Domain.Catalog;
using Nexora.Shared.Errors;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Orders.Support;

/// <summary>
/// US-030 §4, cenário "Preço aplicado por canal" combinado com US-013 (meio a meio): quando um
/// item tem frações, o preço UNITÁRIO do item não é o preço da variante "molde" enviada em
/// <c>variantId</c> — é o preço calculado por <see cref="FractionPricingCalculator"/> a partir do
/// preço vigente (no canal do pedido) de CADA fração, pela regra configurada do tenant
/// (<see cref="FractionPriceRuleResolver"/>, RN-009: maior valor / média / proporcional ao peso).
/// Sem item nenhum sem fração, o preço é simplesmente o da própria variante
/// (<see cref="OrderItemPriceResolver"/>) — por isso este helper cobre os DOIS casos, reaproveitado
/// por <c>CreateOrderCommandHandler</c>/<c>AddItemToOrderCommandHandler</c>/<c>AddOrderItemCommandHandler</c>
/// em vez de cada um duplicar a mesma sequência de passos (mesmo raciocínio de
/// <c>PreviewFractionPricingQueryHandler</c>, US-013 — este helper é o "preview" virando "cálculo
/// de verdade" no momento de gravar o item).
/// </summary>
public static class OrderItemFractionPricing
{
    public sealed record ResolvedFraction(ProductVariant Variant, decimal Weight, decimal UnitPrice);

    public sealed record Resolution(decimal UnitPrice, IReadOnlyList<ResolvedFraction> Fractions);

    /// <summary>
    /// <paramref name="fractionInputs"/> vazio/nulo devolve o preço da PRÓPRIA <paramref name="variantId"/>
    /// (item sem fração) — <paramref name="tenantOperationJson"/> só é consultado quando há fração.
    /// </summary>
    public static async Task<Result<Resolution>> ResolveAsync(
        IApplicationDbContext db,
        Guid tenantId,
        Channel channel,
        Guid variantId,
        IReadOnlyList<AddOrderItemFractionInput>? fractionInputs,
        string? tenantOperationJson,
        CancellationToken cancellationToken)
    {
        if (fractionInputs is null || fractionInputs.Count == 0)
        {
            var ownPrice = await OrderItemPriceResolver.ResolveAsync(db, variantId, tenantId, channel, cancellationToken);
            return ownPrice is null
                ? Result<Resolution>.Failure("Este item não tem preço vigente cadastrado.", ApiErrorCodes.OrderItemVariantPriceNotFound)
                : Result<Resolution>.Success(new Resolution(ownPrice.Value, Array.Empty<ResolvedFraction>()));
        }

        var variantIds = fractionInputs.Select(f => f.VariantId).Distinct().ToList();

        var variants = await db.ProductVariants
            .Include(v => v.Product)
            .Where(v => v.TenantId == tenantId && v.DeletedAt == null && variantIds.Contains(v.Id))
            .ToListAsync(cancellationToken);

        if (variants.Count != variantIds.Count)
        {
            return Result<Resolution>.Failure("Variante da fração não encontrada.", ApiErrorCodes.VariantNotFound);
        }

        var variantById = variants.ToDictionary(v => v.Id);

        var notAllowed = variants.FirstOrDefault(v => !v.Product.AllowsFractions);
        if (notAllowed is not null)
        {
            return Result<Resolution>.Failure($"O produto \"{notAllowed.Product.Name}\" não permite fracionamento.", ApiErrorCodes.FractionNotAllowed);
        }

        var maxFractions = variants.Min(v => v.Product.MaxFractions);
        if (fractionInputs.Count > maxFractions)
        {
            return Result<Resolution>.Failure(
                $"Este item permite no máximo {maxFractions} sabor(es), mas {fractionInputs.Count} foram informados.",
                ApiErrorCodes.FractionMaxExceeded);
        }

        var lines = new List<FractionPricingLine>(fractionInputs.Count);
        var resolvedFractions = new List<ResolvedFraction>(fractionInputs.Count);

        foreach (var input in fractionInputs)
        {
            var variant = variantById[input.VariantId];
            var price = await OrderItemPriceResolver.ResolveAsync(db, variant.Id, tenantId, channel, cancellationToken);
            if (price is null)
            {
                return Result<Resolution>.Failure("Fração sem preço vigente cadastrado.", ApiErrorCodes.OrderItemVariantPriceNotFound);
            }

            lines.Add(new FractionPricingLine(variant.Id, input.Weight, price.Value, variant.SizeCode, variant.Product.FractionGroup));
            resolvedFractions.Add(new ResolvedFraction(variant, input.Weight, price.Value));
        }

        var rule = FractionPriceRuleResolver.Resolve(tenantOperationJson);
        var calculation = FractionPricingCalculator.Calculate(lines, rule);
        if (calculation.IsFailure)
        {
            return Result<Resolution>.Failure(calculation.Error!, calculation.Code, calculation.Errors);
        }

        return Result<Resolution>.Success(new Resolution(calculation.Value!.UnitPrice, resolvedFractions));
    }
}
