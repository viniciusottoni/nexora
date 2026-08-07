using System.Text;

namespace Nexora.Application.Tenants.Support;

/// <summary>
/// Cursor opaco de keyset do diretório de estabelecimentos (US-151 §7/§14) — mesmo padrão de
/// <c>Nexora.Application.Audit.Support.AuditLogCursor</c> (Base64 de um par de valores), mas
/// generalizado: o diretório tem QUATRO critérios de ordenação possíveis (<c>sort</c>), cada um com
/// um "valor primário" de natureza diferente (nome, data ou o composto rank+data de "attention"),
/// então o cursor também carrega o nome do critério que o gerou — se o cliente troca de ordenação
/// no meio da navegação (ou o cursor é adulterado/de outra versão), <see cref="Decode"/> rejeita e
/// o chamador trata como primeira página, nunca lança.
/// </summary>
public static class TenantDirectoryCursor
{
    // Unit separator (0x1F, caractere de controle não imprimível) em vez de '|' — nome de
    // estabelecimento pode conter caracteres livres (ex. "Bar | Grill"), diferente de
    // occurred_at/guid do AuditLogCursor, que nunca colidem com '|'.
    private const char Separator = (char)0x1F;

    public static string Encode(TenantDirectorySort sort, string primary, Guid id)
    {
        var raw = $"{sort.ToWireLabel()}{Separator}{primary}{Separator}{id}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }

    /// <summary>
    /// Nulo quando o cursor é vazio/malformado/de outro critério de ordenação — tratado pelo
    /// chamador como "primeira página" (mesma tolerância de <c>AuditLogCursor.Decode</c>).
    /// </summary>
    public static TenantDirectoryCursorValue? Decode(string? cursor, TenantDirectorySort expectedSort)
    {
        if (string.IsNullOrWhiteSpace(cursor))
            return null;

        try
        {
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var parts = raw.Split(Separator, 3);
            if (parts.Length != 3)
                return null;

            if (!string.Equals(parts[0], expectedSort.ToWireLabel(), StringComparison.Ordinal))
                return null;

            if (!Guid.TryParse(parts[2], out var id))
                return null;

            return new TenantDirectoryCursorValue(parts[1], id);
        }
        catch (FormatException)
        {
            return null;
        }
    }
}

/// <summary>Cursor decodificado — <see cref="Primary"/> é interpretado pelo chamador conforme o <c>sort</c> em vigor (nome literal, ticks de data, ou "rank:ticks" para attention).</summary>
public sealed record TenantDirectoryCursorValue(string Primary, Guid Id);
