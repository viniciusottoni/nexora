namespace Nexora.Shared.Errors;

/// <summary>
/// Códigos de erro do módulo de instalação (ADR-021) — cobre tanto o bootstrap/health/heartbeat
/// do lado edge (<c>Nexora.Application.Installation</c>) quanto registro/consumo de token/
/// sincronização inicial do lado nuvem (<c>Nexora.Application.Installations</c>).
/// Os sufixos seguem a convenção de mapeamento por substring de <c>ResultExtensions.MapStatusCode</c>
/// (Api.Edge/Api.Cloud) — não adicione um código novo sem verificar para qual HTTP status ele cai.
/// </summary>
public static partial class ApiErrorCodes
{
    // Registro de instalação (cloud) — RegisterInstallationCommand. Mapeiam por convenção:
    // *_NOT_FOUND -> 404, *_CONFLICT -> 409, *_PERMISSION_DENIED -> 403, *_INVALID_CREDENTIALS -> 401.
    public const string InstallationNotFound = "INSTALLATION_NOT_FOUND";
    public const string InstallationTokenExpired = "INSTALLATION_TOKEN_EXPIRED";
    public const string InstallationTokenConflict = "INSTALLATION_TOKEN_CONFLICT";
    public const string InstallationPublicKeyPermissionDenied = "INSTALLATION_PUBLIC_KEY_PERMISSION_DENIED";

    /// <summary>
    /// Falha genérica do protocolo de autenticação assinada da instalação (cabeçalho ausente,
    /// relógio fora da janela, instalação desconhecida/não instalada, assinatura inválida ou
    /// nonce reaproveitado) — deliberadamente um único código para não revelar qual etapa falhou,
    /// espelhando o <c>InvalidCredentialsError</c> genérico do guard original em TypeScript.
    /// </summary>
    public const string InstallationSignatureInvalidCredentials = "INSTALLATION_SIGNATURE_INVALID_CREDENTIALS";

    // Bootstrap (edge) — ImportBootstrapCommand.
    public const string InstallationBootstrapConfigMissing = "INSTALLATION_BOOTSTRAP_CONFIG_MISSING";
    public const string InstallationBootstrapVersionMismatch = "INSTALLATION_BOOTSTRAP_VERSION_MISMATCH";

    // Sincronização inicial (cloud) — GetInitialSyncPageQuery / GetInstallationSyncHealthQuery
    // reutilizam InstallationNotFound (instalação inexistente ou ainda não instalada).
}
