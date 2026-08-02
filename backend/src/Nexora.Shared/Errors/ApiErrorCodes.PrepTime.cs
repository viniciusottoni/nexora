namespace Nexora.Shared.Errors;

/// <summary>
/// Códigos de erro do módulo de tempo de preparo e praça por produto (US-016, ADR-021).
/// Arquivo próprio (padrão dos demais módulos — ver docstring de <c>ApiErrorCodes.cs</c>) para
/// não conflitar em merge com os outros agentes trabalhando em paralelo em US-012/US-014/US-015
/// sobre o mesmo módulo de Catálogo.
///
/// NOTA para quem mantiver <c>Nexora.Api.Cloud/Infrastructure/ResultExtensions.cs</c> (arquivo
/// central, fora do escopo deste agente — ver docstring daquele arquivo): os três códigos abaixo
/// ainda NÃO têm entrada em <c>MapErrorCode</c>. Diferente de módulos antigos, esse método não
/// tem mais heurística de substring — código não catalogado cai direto no catch-all 500. Até que
/// alguém adicione as três linhas abaixo ao switch de <c>MapErrorCode</c>, toda falha de "não
/// encontrado" desta US retorna 500 em vez de 404:
///   PrepTimeVariantNotFound  → (StatusCodes.Status404NotFound, false, false)
///   PrepTimeProductNotFound  → (StatusCodes.Status404NotFound, false, false)
///   PrepTimeStationNotFound  → (StatusCodes.Status404NotFound, false, false)
/// </summary>
public static partial class ApiErrorCodes
{
    /// <summary>Variação de produto não encontrada (ou de outro tenant — 404, nunca 403, ADR-021).</summary>
    public const string PrepTimeVariantNotFound = "PREP_TIME_VARIANT_NOT_FOUND";

    /// <summary>Produto não encontrado (ou de outro tenant) ao reatribuir a praça de produção.</summary>
    public const string PrepTimeProductNotFound = "PREP_TIME_PRODUCT_NOT_FOUND";

    /// <summary>Praça informada não existe no tenant do produto (US-017).</summary>
    public const string PrepTimeStationNotFound = "PREP_TIME_STATION_NOT_FOUND";
}
