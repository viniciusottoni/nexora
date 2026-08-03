namespace Nexora.Application.Orders.Support;

/// <summary>
/// US-030 §7/§11 — prazo estimado inicial (<c>promisedAt</c>/<c>estimatedMinutes</c>) na criação do
/// pedido. Critério simples e documentado (US-030 §15/3.2: "prazo dinâmico por fila é US-118, Fase
/// 2"): o item mais demorado do pedido (maior <c>prep_minutes</c> da variante) determina a
/// estimativa — a cozinha prepara itens em paralelo entre praças, então o pedido só fica pronto
/// quando o item mais lento terminar, nunca a soma de todos. Função pura, sem I/O.
/// </summary>
public static class OrderPromiseCalculator
{
    public readonly record struct Estimate(int EstimatedMinutes, DateTimeOffset PromisedAt);

    /// <summary><paramref name="prepMinutesPerItem"/> vazio (nunca deveria acontecer — todo pedido válido tem ao menos um item) devolve estimativa zero, sem lançar.</summary>
    public static Estimate Calculate(DateTimeOffset placedAt, IReadOnlyCollection<short> prepMinutesPerItem)
    {
        var estimatedMinutes = prepMinutesPerItem.Count == 0 ? 0 : prepMinutesPerItem.Max();
        return new Estimate(estimatedMinutes, placedAt.AddMinutes(estimatedMinutes));
    }
}
