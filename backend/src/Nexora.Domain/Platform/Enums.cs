namespace Nexora.Domain.Platform;

// Enums nativos do PostgreSQL (documento 00, §3), mapeados como enum nativo via
// Npgsql.MapEnum na configuração do EF Core (documento 13, §2) — não como VARCHAR + CHECK.

/// <summary>Ciclo de vida comercial do tenant (estabelecimento).</summary>
public enum TenantStatus
{
    Trial,
    Active,
    Suspended,
    Cancelled
}

/// <summary>Estado de um usuário da plataforma (app_user).</summary>
public enum UserStatus
{
    Active,
    Inactive,
    Blocked,

    /// <summary>
    /// Convidado (dono recém-provisionado, <see cref="AppUser.Invite"/>) mas ainda sem senha
    /// definida — não pode autenticar até aceitar o convite (Docs/Domain/12 §8: "app_user
    /// status=INVITED"). Adicionado ao FINAL do enum de propósito (US-002): a coluna é mapeada
    /// como integer simples pelo EF Core hoje (ainda não é um enum nativo do Postgres — ver nota
    /// em AppUserConfiguration), então inserir no meio deslocaria os valores 1/2 (Inactive/Blocked)
    /// já persistidos.
    /// </summary>
    Invited
}

/// <summary>Tipo de dispositivo físico pareado a uma loja.</summary>
public enum DeviceType
{
    Pos,
    Kds,
    Waiter,
    Tablet,
    PrinterHost,
    Other
}

/// <summary>Tipo de praça de produção (ex.: forno, chapa, fritadeira).</summary>
public enum StationType
{
    Assembly,
    Oven,
    Grill,
    Fry,
    Bar,
    Dessert,
    Other
}
