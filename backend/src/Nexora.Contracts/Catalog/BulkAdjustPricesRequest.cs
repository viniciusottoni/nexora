namespace Nexora.Contracts.Catalog;

/// <summary>
/// Corpo de <c>POST /v1/catalog/prices/bulk-adjust</c> (US-014 §7) — reajuste percentual aplicado
/// a todas as variações ativas de uma categoria, em um único canal. <see cref="Percent"/> é um
/// percentual (ex.: <c>8</c> para +8%, <c>-5</c> para -5%) — nunca uma fração.
/// </summary>
public sealed record BulkAdjustPricesRequest(Guid CategoryId, string Channel, decimal Percent);

/// <summary>
/// Resultado do reajuste em massa — <see cref="Updated"/> é a quantidade de variações que
/// efetivamente tiveram um novo preço criado (variações cujo preço calculado seria idêntico ao
/// vigente são no-op e não entram na contagem, mesmo espírito de <c>SetVariantPriceCommandHandler</c>
/// da US-011). <see cref="EffectiveFrom"/> é o <c>ValidFrom</c> comum de todas as linhas criadas
/// nesta chamada.
/// </summary>
public sealed record BulkAdjustPricesResponse(int Updated, DateTimeOffset EffectiveFrom);
