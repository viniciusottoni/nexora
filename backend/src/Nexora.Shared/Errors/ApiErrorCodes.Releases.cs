namespace Nexora.Shared.Errors;

/// <summary>
/// Códigos de erro do módulo de atualização controlada do parque (US-146, ADR-021).
/// </summary>
/// <remarks>
/// Por instrução explícita desta tarefa, <c>ResultExtensions.MapErrorCode</c> (Api.Edge e
/// Api.Cloud) NÃO foi editado aqui — mesma decisão já tomada para <c>ApiErrorCodes.TenantDomains</c>
/// (ver docstring daquele arquivo) para não editar em paralelo com outro agente. Os dois códigos
/// abaixo caem hoje no <c>default</c> daquele switch (500 Internal Server Error) até alguém
/// adicionar as entradas. Mapeamento HTTP pretendido, para quem for adicionar:
/// <list type="table">
/// <item><term><see cref="ReleaseNotFound"/></term><description>404 Not Found, recoverable=false, requiresAuthorization=false — nenhuma <c>Release</c> publicada com essa <c>version</c>.</description></item>
/// <item><term><see cref="ReleaseRolloutCannotDecrease"/></term><description>422 Unprocessable Entity, recoverable=true, requiresAuthorization=false — republicar a mesma versão com <c>rolloutPercent</c> menor que o já liberado (<c>Release.ExpandRollout</c> "nunca reduz", US-146 §3.1); o cliente pode tentar de novo com um percentual maior ou igual.</description></item>
/// </list>
/// </remarks>
public static partial class ApiErrorCodes
{
    /// <summary>Nenhuma <c>release</c> publicada com a versão informada (<c>GetReleaseRolloutQuery</c>).</summary>
    public const string ReleaseNotFound = "RELEASE_NOT_FOUND";

    /// <summary>
    /// RN US-146 §3.1 "liberação gradual": <c>Release.ExpandRollout</c> nunca reduz o percentual já
    /// liberado — republicar a mesma versão com um <c>rolloutPercent</c> MENOR que o atual é rejeitado
    /// explicitamente (em vez de silenciosamente virar um no-op, que esconderia do operador de
    /// plataforma que o pedido não teve o efeito que ele imaginou).
    /// </summary>
    public const string ReleaseRolloutCannotDecrease = "RELEASE_ROLLOUT_CANNOT_DECREASE";
}
