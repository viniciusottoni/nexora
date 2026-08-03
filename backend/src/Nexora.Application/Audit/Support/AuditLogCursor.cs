using System.Globalization;

namespace Nexora.Application.Audit.Support;

/// <summary>
/// Cursor opaco de <c>(occurred_at, id)</c> (US-091 §3, "paginação por cursor, nunca offset") —
/// keyset real, não o número de página codificado: cada página seguinte parte do último par
/// (occurred_at, id) já visto, o que continua estável mesmo com novas linhas chegando entre uma
/// página e outra (diferente de <c>OFFSET</c>, que pula ou repete linha quando o conjunto muda).
/// </summary>
public static class AuditLogCursor
{
    private const char Separator = '|';

    public static string Encode(DateTimeOffset occurredAt, Guid id)
    {
        var raw = $"{occurredAt.UtcTicks}{Separator}{id}";
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(raw));
    }

    /// <summary>Nulo quando o cursor é vazio/malformado — tratado pelo chamador como "primeira página".</summary>
    public static (DateTimeOffset OccurredAt, Guid Id)? Decode(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return null;
        }

        try
        {
            var raw = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var parts = raw.Split(Separator, 2);
            if (parts.Length != 2)
            {
                return null;
            }

            if (!long.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks))
            {
                return null;
            }

            if (!Guid.TryParse(parts[1], out var id))
            {
                return null;
            }

            return (new DateTimeOffset(ticks, TimeSpan.Zero), id);
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
