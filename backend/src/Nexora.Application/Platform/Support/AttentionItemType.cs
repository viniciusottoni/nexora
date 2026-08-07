namespace Nexora.Application.Platform.Support;

/// <summary>
/// US-157 · Central operacional, auditoria e atalhos de suporte — tipo de pendência da fila de
/// atenção (Gherkin "Priorização explicável": "instalação offline, convite expirado e
/// provisionamento parado"). <see cref="InstallationDegraded"/> é um quarto tipo, mais brando,
/// incluído para que a fila tenha gradação real de severidade visível (ver docstring de
/// <see cref="AttentionSeverity"/>) em vez de só dois níveis binários por tipo.
/// </summary>
public enum AttentionItemType
{
    InstallationOffline,
    InstallationDegraded,
    InviteExpired,
    ProvisioningStalled
}

public static class AttentionItemTypeExtensions
{
    public static string ToWireLabel(this AttentionItemType type) => type switch
    {
        AttentionItemType.InstallationOffline => "INSTALLATION_OFFLINE",
        AttentionItemType.InstallationDegraded => "INSTALLATION_DEGRADED",
        AttentionItemType.InviteExpired => "INVITE_EXPIRED",
        AttentionItemType.ProvisioningStalled => "PROVISIONING_STALLED",
        _ => "INSTALLATION_OFFLINE"
    };
}
