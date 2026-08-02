namespace Nexora.Shared.Errors;

/// <summary>Códigos de erro do módulo de praças de produção (stations) — US-017 (ADR-021).</summary>
public static partial class ApiErrorCodes
{
    public const string StationNotFound = "STATION_NOT_FOUND";
    public const string StationCodeAlreadyExists = "STATION_CODE_ALREADY_EXISTS";

    /// <summary>Exclusão recusada porque existem produtos vinculados (US-017 §4) — exige reatribuir os produtos antes.</summary>
    public const string StationHasLinkedProducts = "STATION_HAS_LINKED_PRODUCTS";

    /// <summary>Sessão autenticada sem loja definida no contexto — praça é sempre criada/alterada em uma loja.</summary>
    public const string StationStoreContextMissing = "STATION_STORE_CONTEXT_MISSING";
}
