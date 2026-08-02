namespace Nexora.Shared.Errors;

/// <summary>
/// Catálogo central de códigos de erro estável (ADR-021). Classe <c>partial</c> — cada módulo
/// (Auth, Devices, Installation, Tenants, Roles, Branding, Backups, ...) contribui com o seu
/// próprio arquivo <c>ApiErrorCodes.&lt;Modulo&gt;.cs</c> para evitar conflito de merge.
/// Mapeamento código → HTTP status vive em <c>ResultExtensions</c> de cada Api (Edge/Cloud).
/// </summary>
public static partial class ApiErrorCodes
{
    /// <summary>
    /// Catch-all para erro não mapeado (ADR-021: "Erro não mapeado vira INTERNAL_ERROR com
    /// traceId, sem expor stack trace"). Valor corrigido de "UNKNOWN_ERROR" para "INTERNAL_ERROR"
    /// — o nome antigo não batia com o literal do ADR-021 nem com <c>errorCodeSchema</c> em
    /// packages/contracts/src/errors.ts (gap de contrato corrigido nesta tarefa).
    /// </summary>
    public const string UnknownError = "INTERNAL_ERROR";

    /// <summary>
    /// Falha de validação de entrada (FluentValidation, via ValidationBehavior). Valor corrigido
    /// de "VALIDATION_ERROR" para "VALIDATION_FAILED" — o nome antigo não batia com o exemplo do
    /// ADR-021 (família <c>VALIDATION_*</c>) nem com <c>errorCodeSchema</c> em
    /// packages/contracts/src/errors.ts.
    /// </summary>
    public const string ValidationError = "VALIDATION_FAILED";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string NotFound = "NOT_FOUND";
    public const string Conflict = "CONFLICT";

    // Idempotência — ADR-020
    public const string IdempotencyKeyRequired = "IDEMPOTENCY_KEY_REQUIRED";
    public const string IdempotencyKeyReused = "IDEMPOTENCY_KEY_REUSED";
    public const string RequestInProgress = "REQUEST_IN_PROGRESS";

    // Multi-tenant — ADR-004
    public const string TenantContextMissing = "TENANT_CONTEXT_MISSING";
    public const string TenantInactive = "TENANT_INACTIVE";
}
