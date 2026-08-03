namespace Nexora.Application.Orders.Support;

/// <summary>
/// US-030 §8/ADR-016 — código curto do pedido no formato <c>{letra}{sequência}</c> (ex.: "A47"),
/// equivalente C# da função <c>next_short_code(store, business_day)</c> de
/// <c>Docs/Domain/11-Views-e-Funcoes.md</c>: a letra vem do dia do ano (rotaciona A..Z a cada 26
/// dias, só para variar visualmente o prefixo entre dias — a unicidade real é
/// <c>uq_order_short_code (store_id, business_day, short_code)</c>, não a letra), e a sequência é o
/// maior número já usado nesse dia operacional da loja, mais um. Função pura — quem lê os códigos
/// já usados no dia (I/O, sujeito a concorrência entre lojas/processos) é
/// <c>Nexora.Infrastructure.Persistence.OrderShortCodeAllocator</c> (só Infrastructure pode falar
/// com Npgsql/SQL cru para o lock consultivo que serializa a concorrência, ADR-039).
/// </summary>
public static class OrderShortCodeGenerator
{
    public static char ResolvePrefix(DateOnly businessDay) => (char)('A' + (businessDay.DayOfYear % 26));

    /// <summary>Maior sequência já usada nesse prefixo, mais um — 1 quando nenhum código do dia usa o prefixo ainda.</summary>
    public static int NextSequence(IReadOnlyCollection<string> existingCodesForDay, char prefix)
    {
        var max = 0;

        foreach (var code in existingCodesForDay)
        {
            if (code.Length > 1 &&
                char.ToUpperInvariant(code[0]) == char.ToUpperInvariant(prefix) &&
                int.TryParse(code.AsSpan(1), out var sequence) &&
                sequence > max)
            {
                max = sequence;
            }
        }

        return max + 1;
    }

    public static string BuildCode(char prefix, int sequence) => $"{prefix}{sequence}";
}
