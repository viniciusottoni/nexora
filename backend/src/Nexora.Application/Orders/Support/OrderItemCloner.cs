using Nexora.Domain.Operation;

namespace Nexora.Application.Orders.Support;

/// <summary>
/// Extrai a COMPOSIÇÃO (modificadores, frações e observações) de um <see cref="OrderItem"/> de
/// origem, sem tocar banco — usado por <c>RepeatOrderItemCommandHandler</c> (US-028) para montar o
/// item repetido, e testável em unidade sem <c>IApplicationDbContext</c> (US-028 §12: "cópia fiel
/// de frações, modificadores e observações"). Deliberadamente não resolve preço vigente aqui —
/// isso depende de consulta a <c>Price</c>/<c>Modifier</c> (US-028: "preço vigente, não o preço do
/// item original"), responsabilidade do handler que TEM acesso ao banco.
/// </summary>
public static class OrderItemCloner
{
    public sealed record ModifierSelection(Guid ModifierId, short Quantity);

    public sealed record FractionSelection(Guid VariantId, decimal Weight, short SortOrder);

    public static IReadOnlyList<ModifierSelection> CopyModifiers(OrderItem source) =>
        source.Modifiers
            .Select(m => new ModifierSelection(m.ModifierId, m.Quantity))
            .ToList();

    public static IReadOnlyList<FractionSelection> CopyFractions(OrderItem source) =>
        source.Fractions
            .OrderBy(f => f.SortOrder)
            .Select(f => new FractionSelection(f.VariantId, f.Weight, f.SortOrder))
            .ToList();

    public static string? CopyNotes(OrderItem source) => source.Notes;
}
