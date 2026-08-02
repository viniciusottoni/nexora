namespace Nexora.Application.Catalog.Prices.Commands.BulkAdjustPricesByCategory;

/// <summary>
/// Cálculo puro do reajuste percentual (US-014 §12, "Cálculo de reajuste percentual com
/// arredondamento em centavos") — sem I/O, coberto isoladamente por
/// <c>Nexora.UnitTests.Catalog.BulkPriceAdjustmentCalculatorTests</c>. Arredondamento half-up em 2
/// casas decimais (ADR-017), mesmo padrão de <c>Math.Round(..., MidpointRounding.AwayFromZero)</c>
/// já usado em <c>InventoryCountItem</c>/<c>BrandingContrast</c> no restante do código.
/// </summary>
public static class BulkPriceAdjustmentCalculator
{
    /// <summary>
    /// Aplica <paramref name="percent"/> (ex.: <c>8</c> para +8%, <c>-5</c> para -5%) sobre
    /// <paramref name="currentAmount"/>, arredondando half-up para 2 casas decimais.
    /// </summary>
    public static decimal Apply(decimal currentAmount, decimal percent)
    {
        var factor = 1 + (percent / 100m);
        return Math.Round(currentAmount * factor, 2, MidpointRounding.AwayFromZero);
    }
}
