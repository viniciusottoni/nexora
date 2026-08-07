namespace Nexora.Application.Platform.Support;

/// <summary>
/// US-157 · Central operacional, auditoria e atalhos de suporte — chave opaca de um item da fila de
/// atenção: <c>{TIPO}|{tenantId}|{sourceId}</c>. A fila inteira é PROJETADA (nenhuma tabela própria
/// de item), então o <c>itemId</c> exposto por <c>GetAttentionQueueQuery</c> precisa carregar tudo
/// que <c>POST /v1/platform/attention/{itemId}/acknowledgements</c> vai precisar para localizar o
/// tenant SEM outra consulta prévia: <see cref="AdministrativeAttentionAcknowledgement"/> (a única
/// tabela nova desta US) tem RLS por <c>tenant_id</c> (ADR-004) — sem o tenant já embutido na chave,
/// o handler de reconhecimento não teria como fixar <c>app.tenant_id</c> antes de gravar. Não é
/// Base64 (diferente de <c>TenantDirectoryCursor</c>/<c>AuditLogCursor</c>): não há dado sensível
/// aqui, só GUIDs e um rótulo de tipo — a legibilidade em log/depuração vale mais que a opacidade.
/// </summary>
public static class AttentionItemId
{
    private const char Separator = '|';

    public static string Encode(AttentionItemType type, Guid tenantId, Guid sourceId) =>
        $"{type.ToWireLabel()}{Separator}{tenantId}{Separator}{sourceId}";

    /// <summary><c>null</c> quando a chave é malformada ou o tipo não é reconhecido — o chamador trata isso como <c>ATTENTION_ITEM_NOT_FOUND</c>, nunca como exceção.</summary>
    public static AttentionItemIdValue? TryDecode(string? itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return null;

        var parts = itemId.Split(Separator, 3);
        if (parts.Length != 3)
            return null;

        var type = parts[0] switch
        {
            "INSTALLATION_OFFLINE" => AttentionItemType.InstallationOffline,
            "INSTALLATION_DEGRADED" => AttentionItemType.InstallationDegraded,
            "INVITE_EXPIRED" => AttentionItemType.InviteExpired,
            "PROVISIONING_STALLED" => AttentionItemType.ProvisioningStalled,
            _ => (AttentionItemType?)null
        };

        if (type is null)
            return null;

        if (!Guid.TryParse(parts[1], out var tenantId))
            return null;

        if (!Guid.TryParse(parts[2], out var sourceId))
            return null;

        return new AttentionItemIdValue(type.Value, tenantId, sourceId);
    }
}

public sealed record AttentionItemIdValue(AttentionItemType Type, Guid TenantId, Guid SourceId);
