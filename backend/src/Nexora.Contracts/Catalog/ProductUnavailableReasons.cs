namespace Nexora.Contracts.Catalog;

/// <summary>
/// Lista curta e fixa de motivos de indisponibilidade (US-044 §10: "motivo escolhido por número
/// (1 acabou, 2 equipamento, 3 qualidade), não por texto" — a cozinha usa teclado numérico, zero
/// digitação livre). Substitui o texto livre que a US-015 original aceitava em
/// <see cref="MarkProductUnavailableRequest.Reason"/>/<c>SetProductAvailabilityRequest.Reason</c> —
/// os dois processos (KDS e painel) passam a validar contra a MESMA lista, porque os dois batem no
/// mesmo <c>MarkProductUnavailableCommand</c>/<c>MarkProductUnavailableCommandValidator</c> (ver
/// docstring do Command: "o mesmo comando serve os dois processos").
///
/// Persistido como <c>string</c> em <c>product.unavailable_reason</c> (Domain não muda — 3 valores
/// fixos validados aqui, na borda de Application, não justificam promover a coluna a enum nativo do
/// Postgres agora). Mesmo espírito de <see cref="Nexora.Domain.Metrics.AlertTypes"/>: strings
/// constantes compartilhadas, não enum C#, para não introduzir conversão em cada camada.
/// </summary>
public static class ProductUnavailableReasons
{
    /// <summary>Tecla 1 — acabou o insumo.</summary>
    public const string OutOfStock = "OUT_OF_STOCK";

    /// <summary>Tecla 2 — equipamento indisponível (forno, fritadeira etc.).</summary>
    public const string Equipment = "EQUIPMENT";

    /// <summary>Tecla 3 — problema de qualidade do insumo/prato.</summary>
    public const string Quality = "QUALITY";

    /// <summary>Ordem de exibição/tecla no KDS — índice 0 é a tecla "1", índice 1 é a tecla "2" etc.</summary>
    public static readonly IReadOnlyList<string> All = new[] { OutOfStock, Equipment, Quality };

    public static bool IsValid(string? reason) => reason is not null && All.Contains(reason);
}
