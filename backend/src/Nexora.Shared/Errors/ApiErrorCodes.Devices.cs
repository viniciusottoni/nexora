namespace Nexora.Shared.Errors;

/// <summary>
/// Códigos de erro do módulo de dispositivos e pareamento (ADR-021). Porta de
/// <c>packages/domain/src/devices/device-registry.ts</c> — PairingCodeRejectedError,
/// PairingRateLimitError, DeviceValidationError e NotFoundError (este último cai no
/// <see cref="ApiErrorCodes.NotFound"/> genérico já existente, não repetido aqui).
///
/// NOTA para quem mantiver Nexora.Api.Edge/Api.Cloud "Infrastructure/ResultExtensions.cs"
/// (ver docstring daquele arquivo — atualização central, não por módulo): os códigos abaixo
/// ainda não têm entrada explícita no mapeamento de status HTTP. Pela convenção por
/// substring já existente, <see cref="DeviceNotFound"/> já cai em 404 (contém "NOT_FOUND");
/// os demais caem no default 400 até que alguém adicione, idealmente:
///   DevicePairingCodeInvalid  → 403 (era PairingCodeRejectedError, status 403 no TS)
///   DevicePairingCodeExpired  → 403 (idem)
///   DevicePairingCodeConsumed → 409 (conflito de estado — código já usado)
///   DevicePairingRateLimited  → 429 (era PairingRateLimitError, status 429 no TS)
/// </summary>
public static partial class ApiErrorCodes
{
    /// <summary>Código de pareamento não corresponde a nenhum código ativo (hash não encontrado).</summary>
    public const string DevicePairingCodeInvalid = "DEVICE_PAIRING_CODE_INVALID";

    /// <summary>Código de pareamento encontrado, porém sua janela de validade (10 min) já passou.</summary>
    public const string DevicePairingCodeExpired = "DEVICE_PAIRING_CODE_EXPIRED";

    /// <summary>Código de pareamento encontrado, porém já foi consumido por outro dispositivo.</summary>
    public const string DevicePairingCodeConsumed = "DEVICE_PAIRING_CODE_CONSUMED";

    /// <summary>
    /// Limite de tentativas de pareamento excedido para a loja (5 tentativas / 15 min) —
    /// porta de PairingRateLimitError (pairing-rate-limiter.ts).
    /// </summary>
    public const string DevicePairingRateLimited = "DEVICE_PAIRING_RATE_LIMIT";

    /// <summary>Dispositivo não encontrado (ou pertence a outro tenant — 404, nunca 403, ver ADR-021).</summary>
    public const string DeviceNotFound = "DEVICE_NOT_FOUND";

    /// <summary>Ação exige um gestor autenticado no contexto (porta de requireActor em device-registry.ts).</summary>
    public const string DeviceActorRequired = "DEVICE_ACTOR_REQUIRED";

    /// <summary>Loja não resolvida no contexto da requisição (porta de requireStore em device-registry.ts).</summary>
    public const string DeviceStoreContextMissing = "DEVICE_STORE_CONTEXT_MISSING";

    /// <summary>Exclusão recusada: o dispositivo ainda está ativo — precisa ser revogado antes de sair da listagem.</summary>
    public const string DeviceMustBeRevokedBeforeDelete = "DEVICE_MUST_BE_REVOKED_BEFORE_DELETE";
}
