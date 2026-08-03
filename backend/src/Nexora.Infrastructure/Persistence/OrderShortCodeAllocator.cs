using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Orders.Support;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Infrastructure.Persistence;

/// <summary>
/// Implementação real de <see cref="IOrderShortCodeAllocator"/> — só Infrastructure pode falar SQL
/// cru com o Postgres (ADR-039). Usa <c>pg_advisory_xact_lock</c> (lock consultivo escopado à
/// transação corrente, liberado automaticamente em commit/rollback) para serializar a alocação por
/// <c>(storeId, businessDay)</c>: duas requisições concorrentes de pedido na MESMA loja e no MESMO
/// dia operacional nunca leem o mesmo "maior código já usado" ao mesmo tempo, porque a segunda
/// espera a primeira liberar o lock (ao terminar sua transação) antes de calcular a própria
/// sequência — elimina a corrida sem precisar de retentativa por exceção de violação de unicidade.
/// </summary>
public sealed class OrderShortCodeAllocator : IOrderShortCodeAllocator
{
    private readonly AppDbContext _db;

    public OrderShortCodeAllocator(AppDbContext db)
    {
        _db = db;
    }

    public async Task<string> AllocateAsync(Guid storeId, DateOnly businessDay, CancellationToken cancellationToken)
    {
        var lockKey = ComputeLockKey(storeId, businessDay);
        await _db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({lockKey})", cancellationToken);

        var prefix = OrderShortCodeGenerator.ResolvePrefix(businessDay);
        var existingCodes = await _db.Orders
            .AsNoTracking()
            .Where(o => o.StoreId == storeId && o.BusinessDay == businessDay)
            .Select(o => o.ShortCode)
            .ToListAsync(cancellationToken);

        var sequence = OrderShortCodeGenerator.NextSequence(existingCodes, prefix);
        return OrderShortCodeGenerator.BuildCode(prefix, sequence);
    }

    /// <summary>
    /// <c>pg_advisory_xact_lock</c> exige uma chave <c>bigint</c> — combina os dois valores num
    /// hash estável dentro do processo (não precisa sobreviver a restart, só precisa ser igual
    /// para a MESMA loja+dia dentro da mesma janela de concorrência real).
    /// </summary>
    private static long ComputeLockKey(Guid storeId, DateOnly businessDay)
    {
        unchecked
        {
            var hash = 17L;
            hash = (hash * 397) ^ storeId.GetHashCode();
            hash = (hash * 397) ^ businessDay.DayNumber;
            return hash;
        }
    }
}
