namespace Nexora.Contracts.Installations;

/// <summary>
/// GET /v1/sync/pull (cloud, autenticado pelo protocolo de assinatura da instalação). Envelope
/// de página única — espelha <c>InitialSyncPage</c>/<c>PrismaInitialSyncReader</c> originais:
/// um único "evento" sintético <c>tenant.config_updated</c> cujo payload carrega uma fatia da
/// configuração + catálogo + autorização, com paginação por cursor sobre a concatenação lógica
/// de todas as listas (ver <c>GetInitialSyncPageQueryHandler</c> para o algoritmo de janelamento).
/// </summary>
public sealed record InitialSyncPageResponse(
    IReadOnlyList<InitialSyncEvent> Events,
    int NextCursor,
    bool HasMore);

public sealed record InitialSyncEvent(Guid Id, string Type, InitialSyncBootstrapPayload Payload);

public sealed record InitialSyncBootstrapPayload(
    int ConfigVersion,
    int CatalogVersion,
    string Branding,
    string Operation,
    string Thresholds,
    string Modules,
    string Fiscal,
    string Printers,
    string Payments,
    string Maintenance,
    InitialSyncCatalogPayload Catalog,
    InitialSyncAuthorizationPayload Authorization);

public sealed record InitialSyncCatalogPayload(
    IReadOnlyList<InitialSyncStation> Stations,
    IReadOnlyList<InitialSyncCategory> Categories,
    IReadOnlyList<InitialSyncProduct> Products,
    IReadOnlyList<InitialSyncProductVariant> Variants,
    IReadOnlyList<InitialSyncPrice> Prices,
    IReadOnlyList<InitialSyncModifierGroup> ModifierGroups,
    IReadOnlyList<InitialSyncModifier> Modifiers,
    IReadOnlyList<InitialSyncProductModifierGroup> ProductModifierGroups);

public sealed record InitialSyncAuthorizationPayload(
    IReadOnlyList<InitialSyncRole> Roles,
    IReadOnlyList<InitialSyncOperationalUser> Users,
    IReadOnlyList<InitialSyncUserRole> UserRoles);

public sealed record InitialSyncStation(
    Guid Id, Guid TenantId, Guid StoreId, string Code, string Name, string Type,
    short? CapacitySlots, int? AvgCookSeconds, short SortOrder, bool IsActive,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, DateTimeOffset? DeletedAt);

public sealed record InitialSyncCategory(
    Guid Id, Guid TenantId, string Name, string? Description, short SortOrder,
    string? AvailableSchedule, bool IsActive,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, DateTimeOffset? DeletedAt);

public sealed record InitialSyncProduct(
    Guid Id, Guid TenantId, Guid CategoryId, Guid? StationId, string Name, string? Description,
    string? IngredientsText, IReadOnlyList<string> Allergens, short SortOrder, bool IsActive,
    bool IsAvailable, string? UnavailableReason, DateTimeOffset? UnavailableSince,
    bool AllowsFractions, short MaxFractions, string? FractionGroup,
    string? Ncm, string? Cest, string? Cfop, short? OriginCode,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, DateTimeOffset? DeletedAt);

public sealed record InitialSyncProductVariant(
    Guid Id, Guid TenantId, Guid ProductId, string Name, string? Sku, string? SizeCode,
    short PrepMinutes, bool IsDefault, bool IsActive, string? FiscalRates,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, DateTimeOffset? DeletedAt);

/// <summary>Amount em <c>decimal</c> (ADR-017) — a serialização como string fica a cargo do conversor global de JSON.</summary>
public sealed record InitialSyncPrice(
    Guid Id, Guid TenantId, Guid VariantId, string Channel, decimal Amount,
    DateTimeOffset ValidFrom, DateTimeOffset? ValidTo, DateTimeOffset CreatedAt, Guid? CreatedBy);

public sealed record InitialSyncModifierGroup(
    Guid Id, Guid TenantId, string Name, short MinSelect, short MaxSelect, bool IsRequired,
    short SortOrder, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, DateTimeOffset? DeletedAt);

/// <summary>PriceDelta/Quantity em <c>decimal</c> (ADR-017) — mesma nota de <see cref="InitialSyncPrice"/>.</summary>
public sealed record InitialSyncModifier(
    Guid Id, Guid TenantId, Guid GroupId, string Name, decimal PriceDelta, Guid? IngredientId,
    decimal? Quantity, bool IsAvailable, short SortOrder,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, DateTimeOffset? DeletedAt);

public sealed record InitialSyncProductModifierGroup(Guid TenantId, Guid ProductId, Guid GroupId, short SortOrder);

public sealed record InitialSyncRole(
    Guid Id, Guid TenantId, string Code, string Name, string Permissions, bool IsSystem,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, DateTimeOffset? DeletedAt);

/// <summary>
/// Só os campos necessários para login por PIN offline — nunca inclui email, hash de senha ou
/// segredo MFA (mesma exclusão deliberada do <c>PrismaInitialSyncReader</c> original; usuários
/// administrativos de painel não sincronizam para o edge).
/// </summary>
public sealed record InitialSyncOperationalUser(
    Guid Id, Guid TenantId, string Name, string PinHash, string? PinLookup, string Status,
    DateTimeOffset? PinRotatedAt, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, DateTimeOffset? DeletedAt);

public sealed record InitialSyncUserRole(Guid Id, Guid TenantId, Guid UserId, Guid RoleId, Guid? StoreId);
