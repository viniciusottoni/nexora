namespace Nexora.Shared.Errors;

/// <summary>
/// US-156 · Recuperação do provisionamento e token de instalação. Arquivo NOVO (ver docstring de
/// <see cref="ApiErrorCodes"/> e de <c>ApiErrorCodes.Installation.cs</c>, que permanece intocado por
/// esta tarefa) — <c>ResultExtensions.MapErrorCode</c> (Api.Cloud/Api.Edge) ainda não conhece estes
/// dois códigos; até a integração central adicioná-los ao switch, eles caem no catch-all (500),
/// conforme documentado no relatório desta tarefa.
/// </summary>
public static partial class ApiErrorCodes
{
    /// <summary>
    /// Gherkin "Token já consumido" — a instalação já concluiu o pareamento
    /// (<see cref="Nexora.Domain.Platform.EdgeInstallation.IsInstalled"/>); reemitir o token
    /// inicial não tem efeito útil. 409 — direciona ao fluxo de manutenção do parque já instalado
    /// (US-146), nunca a uma segunda tentativa de "recuperar" o mesmo provisionamento.
    /// </summary>
    public const string InstallationAlreadyRegistered = "INSTALLATION_ALREADY_REGISTERED";

    /// <summary>Nenhuma linha de <c>installation_credential</c> encontrada para o par (installationId, credentialId) da rota de revogação. 404.</summary>
    public const string InstallationCredentialNotFound = "INSTALLATION_CREDENTIAL_NOT_FOUND";
}
