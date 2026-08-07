namespace Nexora.Shared.Errors;

/// <summary>
/// US-154 · Gestão de planos e configuração comercial — códigos de erro do módulo de planos
/// comerciais (ADR-021). Arquivo NOVO (em vez de estender <c>ApiErrorCodes.Tenants.cs</c>) para não
/// colidir com outras histórias da E-15 editando o mesmo arquivo em paralelo — mesma convenção dos
/// demais partial files desta tarefa.
/// </summary>
public static partial class ApiErrorCodes
{
    /// <summary>
    /// Código ausente do catálogo ativo (inexistente OU desativado) — 422, US-154 §4 cenário
    /// "Plano desconhecido". O mapeamento HTTP real (status/recoverable/requiresAuthorization) só
    /// fecha quando <c>ResultExtensions.MapErrorCode</c> (Nexora.Api.Cloud/Nexora.Api.Edge) for
    /// atualizado na integração central desta tarefa — ver relatório final para detalhes.
    /// </summary>
    public const string PlanNotAvailable = "PLAN_NOT_AVAILABLE";
}
