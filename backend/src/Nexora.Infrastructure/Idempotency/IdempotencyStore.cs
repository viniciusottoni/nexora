using Nexora.Application.Abstractions.Idempotency;
using Nexora.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Infrastructure.Idempotency;

/// <summary>
/// Implementação de <see cref="IIdempotencyStore"/> sobre <see cref="AppDbContext"/> (ADR-020).
/// </summary>
/// <remarks>
/// <para>
/// Injeta <see cref="AppDbContext"/> concreto, não <c>IApplicationDbContext</c>: <see cref="BeginAsync"/>
/// precisa de um <c>INSERT ... ON CONFLICT (key) DO UPDATE ... WHERE</c> atômico (reservar a
/// chave sem duas requisições concorrentes colidirem, ADR-020 "chave em processamento" -> 409) e
/// isso não é uma operação expressável só com <c>DbSet&lt;T&gt;</c>/LINQ. Como
/// <c>Nexora.Infrastructure</c> já pode referenciar EF Core/Npgsql (ADR-039), pedir o
/// <c>AppDbContext</c> concreto aqui não vaza Npgsql para <c>Nexora.Application</c> — a porta
/// <see cref="IIdempotencyStore"/> continua sem nenhum tipo de EF Core provider na assinatura.
/// </para>
/// <para>
/// RLS está deliberadamente desligado em <c>idempotency_key</c> (migration
/// <c>AdjustIdempotencyKeyTenantScope</c>, comentário em <c>Nexora.Domain.Platform.IdempotencyKey</c>)
/// — esta classe nunca depende de <c>ICurrentTenantContext</c> ter resolvido um tenant.
/// </para>
/// <para>
/// <b>Sem <c>DELETE</c> físico de propósito</b>: o papel de runtime (<c>app_user_role</c>) nunca
/// recebe privilégio de <c>DELETE</c> em nenhuma tabela (Docs/Domain/10 §4). Abandonar uma reserva
/// (<see cref="DiscardAsync"/>) é um <c>UPDATE</c> para <c>status='FAILED'</c>, e o próprio
/// <c>ON CONFLICT DO UPDATE ... WHERE</c> de <see cref="BeginAsync"/> reconhece esse estado (igual
/// a uma chave expirada) como livre para reclamar com a mesma chave.
/// </para>
/// </remarks>
public sealed class IdempotencyStore : IIdempotencyStore
{
    private readonly AppDbContext _db;

    public IdempotencyStore(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IdempotencyRecord?> FindAsync(string key, CancellationToken cancellationToken)
    {
        var entity = await _db.IdempotencyKeys.AsNoTracking()
            .FirstOrDefaultAsync(k => k.Key == key, cancellationToken);

        return entity is null
            ? null
            : new IdempotencyRecord(entity.Endpoint, entity.RequestHash, entity.Status, entity.ResponseStatus, entity.ResponseBody, entity.ExpiresAt);
    }

    public async Task<IdempotencyBeginOutcome> BeginAsync(
        string key, Guid? tenantId, string endpoint, string requestHash, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        // ON CONFLICT DO UPDATE ... WHERE reclama a chave quando ela não existia, expirou, ou foi
        // abandonada (status='FAILED') — nos três casos o UPDATE roda e 1 linha é afetada; se a
        // chave existe, está dentro da validade e ainda IN_PROGRESS/COMPLETED, a cláusula WHERE
        // não bate e 0 linhas são afetadas (outra requisição está processando ou já terminou).
        var affected = await _db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO idempotency_key (key, tenant_id, endpoint, request_hash, status, response_status, response_body, created_at, expires_at)
            VALUES ({key}, {tenantId}, {endpoint}, {requestHash}, 'IN_PROGRESS', NULL, NULL, now(), {expiresAt})
            ON CONFLICT (key) DO UPDATE SET
                tenant_id = EXCLUDED.tenant_id,
                endpoint = EXCLUDED.endpoint,
                request_hash = EXCLUDED.request_hash,
                status = 'IN_PROGRESS',
                response_status = NULL,
                response_body = NULL,
                created_at = now(),
                expires_at = EXCLUDED.expires_at
            WHERE idempotency_key.status = 'FAILED' OR idempotency_key.expires_at < now()
            """,
            cancellationToken);

        return affected > 0 ? IdempotencyBeginOutcome.Started : IdempotencyBeginOutcome.AlreadyReserved;
    }

    public async Task CompleteAsync(string key, int responseStatus, string? responseBody, CancellationToken cancellationToken)
    {
        var entity = await _db.IdempotencyKeys.FirstOrDefaultAsync(k => k.Key == key, cancellationToken);
        if (entity is null)
        {
            // Não deveria acontecer (BeginAsync sempre grava antes) — mas não é motivo para
            // derrubar a resposta já produzida ao cliente; só não há o que completar.
            return;
        }

        entity.Complete(responseStatus, responseBody);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DiscardAsync(string key, CancellationToken cancellationToken)
    {
        var entity = await _db.IdempotencyKeys.FirstOrDefaultAsync(k => k.Key == key, cancellationToken);
        if (entity is null)
        {
            return;
        }

        entity.Abandon();
        await _db.SaveChangesAsync(cancellationToken);
    }
}
