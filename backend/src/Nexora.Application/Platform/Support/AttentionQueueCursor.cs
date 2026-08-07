using System.Globalization;
using System.Text;

namespace Nexora.Application.Platform.Support;

/// <summary>
/// US-157 · Central operacional, auditoria e atalhos de suporte — cursor opaco de keyset
/// <c>(severityRank, since, itemId)</c> para <c>GetAttentionQueueQuery</c>, mesma técnica de
/// <c>TenantDirectoryCursor</c>/<c>AuditLogCursor</c> (Base64 de valores separados por um caractere
/// de controle não imprimível — <see cref="AttentionItemId.Encode"/> já usa <c>|</c> internamente, então
/// aqui é preciso um separador diferente). A fila inteira é materializada em memória pelo handler
/// (agregação cross-tenant, não uma única query SQL — ver docstring de
/// <c>GetAttentionQueueQueryHandler</c>), então este cursor só precisa ser estável entre chamadas
/// consecutivas da MESMA janela de dados; não há tradução para SQL a fazer aqui.
/// </summary>
public static class AttentionQueueCursor
{
    private const char Separator = (char)0x1F;

    public static string Encode(int severityRank, DateTimeOffset since, string itemId)
    {
        var raw = $"{severityRank}{Separator}{since.UtcTicks}{Separator}{itemId}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }

    /// <summary><c>null</c> quando o cursor é vazio/malformado — tratado pelo chamador como "primeira página".</summary>
    public static AttentionQueueCursorValue? Decode(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
            return null;

        try
        {
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var parts = raw.Split(Separator, 3);
            if (parts.Length != 3)
                return null;

            if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var rank))
                return null;

            if (!long.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks))
                return null;

            return new AttentionQueueCursorValue(rank, new DateTimeOffset(ticks, TimeSpan.Zero), parts[2]);
        }
        catch (FormatException)
        {
            return null;
        }
    }
}

public sealed record AttentionQueueCursorValue(int SeverityRank, DateTimeOffset Since, string ItemId);
