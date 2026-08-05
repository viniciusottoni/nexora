namespace Nexora.Shared.Errors;

/// <summary>
/// Códigos de erro do módulo de acesso de suporte auditado (US-145) — única exceção autorizada
/// ao isolamento da RN-015, por isso os erros abaixo separam explicitamente três motivos
/// diferentes de recusa de token (desconhecido/expirado/revogado) em vez de um único código
/// genérico: cada um alimenta uma métrica de observabilidade distinta (US-145 §11) e o cenário
/// Gherkin "Expiração do token" exige que a tentativa recusada fique registrada com o motivo real.
/// </summary>
public static partial class ApiErrorCodes
{
    /// <summary>
    /// <c>DELETE /v1/tenant/support-access/{id}</c> — id inexistente OU pertencente a outro
    /// tenant (ADR-021, "não revelar que o recurso existe em outro tenant": os dois casos
    /// devolvem exatamente este código/404, nunca 403).
    /// </summary>
    public const string SupportAccessNotFound = "SUPPORT_ACCESS_NOT_FOUND";

    /// <summary>Token de suporte com hash desconhecido — nenhuma concessão corresponde a ele.</summary>
    public const string SupportAccessTokenNotFound = "SUPPORT_ACCESS_TOKEN_NOT_FOUND";

    /// <summary>Token de suporte válido, mas o prazo concedido (duração da concessão) já passou.</summary>
    public const string SupportAccessTokenExpired = "SUPPORT_ACCESS_TOKEN_EXPIRED";

    /// <summary>Token de suporte válido, mas revogado pelo cliente antes do uso.</summary>
    public const string SupportAccessTokenRevoked = "SUPPORT_ACCESS_TOKEN_REVOKED";
}
