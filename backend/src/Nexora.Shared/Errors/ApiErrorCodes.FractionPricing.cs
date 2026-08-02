namespace Nexora.Shared.Errors;

/// <summary>
/// Códigos de erro do preview de precificação de fração (US-013, "Pizza meio a meio com
/// frações"). Cobre só o cálculo/validação de <c>POST /v1/catalog/fraction-pricing/preview</c> —
/// não existe ainda um módulo de Pedidos completo nesta solution (ver decisão de escopo no
/// relatório da tarefa que introduziu este arquivo), então não há códigos de criação/confirmação
/// de pedido aqui.
/// </summary>
public static partial class ApiErrorCodes
{
    /// <summary>Alguma variante informada não existe, não pertence ao tenant, ou está excluída (soft delete).</summary>
    public const string FractionVariantNotFound = "FRACTION_VARIANT_NOT_FOUND";

    /// <summary>O produto dono de uma das variantes escolhidas não tem <c>AllowsFractions</c> habilitado.</summary>
    public const string FractionNotAllowed = "FRACTION_NOT_ALLOWED";

    /// <summary>A quantidade de frações enviada excede o <c>MaxFractions</c> (o menor entre os produtos envolvidos).</summary>
    public const string FractionMaxExceeded = "FRACTION_MAX_EXCEEDED";

    /// <summary>Menos de duas frações foram informadas — não configura um item meio a meio (US-013 §3.1).</summary>
    public const string FractionMinimumNotMet = "FRACTION_MINIMUM_NOT_MET";

    /// <summary>As variantes escolhidas têm <c>SizeCode</c> divergente (US-013 §4, cenário "Tamanhos incompatíveis"). Mesmo valor de string do exemplo em US-013 §7.</summary>
    public const string FractionSizeMismatch = "FRACTION_SIZE_MISMATCH";

    /// <summary>Os produtos das variantes escolhidas têm <c>FractionGroup</c> divergente (US-013 §4, cenário "Grupos de fração distintos").</summary>
    public const string FractionGroupMismatch = "FRACTION_GROUP_MISMATCH";

    /// <summary>A soma dos pesos das frações não fecha em 1,0 (US-013 §4, cenário "Montagem de meio a meio").</summary>
    public const string FractionWeightSumInvalid = "FRACTION_WEIGHT_SUM_INVALID";

    /// <summary>Alguma variante escolhida não tem preço vigente no canal consultado.</summary>
    public const string FractionPriceNotFound = "FRACTION_PRICE_NOT_FOUND";
}
