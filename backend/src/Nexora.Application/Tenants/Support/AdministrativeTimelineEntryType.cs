namespace Nexora.Application.Tenants.Support;

/// <summary>
/// US-157 · Central operacional, auditoria e atalhos de suporte — tipo de fato da linha do tempo
/// administrativa de um tenant (<c>GetAdministrativeTimelineQuery</c>). O mesmo valor é usado tanto
/// no filtro <c>?type=</c> quanto no campo <c>type</c> de cada entrada da resposta — simplificação
/// deliberada em relação ao exemplo ilustrativo da especificação (que sugere nomes curtos agrupados,
/// ex. <c>type=STATUS,PLAN</c>, diferentes do <c>STATUS_CHANGED</c> do item): evita uma segunda
/// taxonomia sem necessidade real, mesma disciplina de "não invente contrato onde o existente já
/// serve" já aplicada por outros módulos desta base.
/// </summary>
public enum AdministrativeTimelineEntryType
{
    Creation,
    StatusChanged,
    PlanChanged,
    OwnerChanged,
    CredentialsReissued,
    DomainRegistered,
    SupportGranted,
    Incident
}

public static class AdministrativeTimelineEntryTypeExtensions
{
    public static string ToWireLabel(this AdministrativeTimelineEntryType type) => type switch
    {
        AdministrativeTimelineEntryType.Creation => "CREATION",
        AdministrativeTimelineEntryType.StatusChanged => "STATUS_CHANGED",
        AdministrativeTimelineEntryType.PlanChanged => "PLAN_CHANGED",
        AdministrativeTimelineEntryType.OwnerChanged => "OWNER_CHANGED",
        AdministrativeTimelineEntryType.CredentialsReissued => "CREDENTIALS_REISSUED",
        AdministrativeTimelineEntryType.DomainRegistered => "DOMAIN_REGISTERED",
        AdministrativeTimelineEntryType.SupportGranted => "SUPPORT_GRANTED",
        AdministrativeTimelineEntryType.Incident => "INCIDENT",
        _ => "CREATION"
    };
}
