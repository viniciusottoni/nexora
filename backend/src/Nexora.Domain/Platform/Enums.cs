namespace Nexora.Domain.Platform;

// Enums nativos do PostgreSQL (documento 00, §3), mapeados como enum nativo via
// Npgsql.MapEnum na configuração do EF Core (documento 13, §2) — não como VARCHAR + CHECK.

/// <summary>
/// Ciclo de vida comercial do tenant (estabelecimento) — US-153, máquina de estados canônica
/// (único enum/contrato usado em todas as camadas, doc §14 DoD). <see cref="Trial"/> foi renomeado
/// para <see cref="Provisioned"/> (mesmo valor 0 já persistido — renomear um rótulo de enum mapeado
/// como <c>integer</c> não exige migração de dado, só recompilar) e <see cref="Installing"/> foi
/// ACRESCENTADO NO FINAL, mesma convenção já usada em <see cref="UserStatus.Invited"/>: inserir no
/// meio deslocaria os valores 1/2/3 (Active/Suspended/Cancelled) já persistidos. Ver
/// <see cref="TenantStatusTransitions"/> para a matriz de transições válidas entre estes cinco
/// estados.
/// </summary>
public enum TenantStatus
{
    /// <summary>Tenant criado, aguardando instalação do servidor edge (renomeado de "Trial").</summary>
    Provisioned,
    Active,
    Suspended,
    Cancelled,

    /// <summary>Instalação edge registrada; aguardando as pré-condições de ativação (checklist + proprietário ativo).</summary>
    Installing
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
