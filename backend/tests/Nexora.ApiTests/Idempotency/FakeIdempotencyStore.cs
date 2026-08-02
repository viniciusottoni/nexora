using Nexora.Application.Abstractions.Idempotency;

namespace Nexora.ApiTests.Idempotency;

/// <summary>
/// Duplo de teste de <see cref="IIdempotencyStore"/> — reproduz em memória exatamente as regras
/// do <c>INSERT ... ON CONFLICT DO UPDATE ... WHERE</c> real de
/// <c>Nexora.Infrastructure.Idempotency.IdempotencyStore</c> (ver
/// <see cref="Nexora.IntegrationTests.IdempotencyStoreTests"/> para a prova contra Postgres real)
/// — bom suficiente para testar a LÓGICA do middleware (leitura de header, decisão de
/// 422/409/replay) sem precisar de banco.
/// </summary>
public sealed class FakeIdempotencyStore : IIdempotencyStore
{
    private sealed class Entry
    {
        public required string Endpoint { get; set; }
        public required string RequestHash { get; set; }
        public string Status { get; set; } = "IN_PROGRESS";
        public int? ResponseStatus { get; set; }
        public string? ResponseBody { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
    }

    private readonly Dictionary<string, Entry> _entries = new();

    public int BeginCallCount { get; private set; }

    public Task<IdempotencyRecord?> FindAsync(string key, CancellationToken cancellationToken)
    {
        if (!_entries.TryGetValue(key, out var entry))
        {
            return Task.FromResult<IdempotencyRecord?>(null);
        }

        return Task.FromResult<IdempotencyRecord?>(new IdempotencyRecord(
            entry.Endpoint, entry.RequestHash, entry.Status, entry.ResponseStatus, entry.ResponseBody, entry.ExpiresAt));
    }

    public Task<IdempotencyBeginOutcome> BeginAsync(
        string key, Guid? tenantId, string endpoint, string requestHash, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        BeginCallCount++;

        if (_entries.TryGetValue(key, out var existing) &&
            existing.Status != "FAILED" &&
            existing.ExpiresAt >= DateTimeOffset.UtcNow)
        {
            return Task.FromResult(IdempotencyBeginOutcome.AlreadyReserved);
        }

        _entries[key] = new Entry
        {
            Endpoint = endpoint,
            RequestHash = requestHash,
            Status = "IN_PROGRESS",
            ExpiresAt = expiresAt,
        };
        return Task.FromResult(IdempotencyBeginOutcome.Started);
    }

    public Task CompleteAsync(string key, int responseStatus, string? responseBody, CancellationToken cancellationToken)
    {
        if (_entries.TryGetValue(key, out var entry))
        {
            entry.Status = "COMPLETED";
            entry.ResponseStatus = responseStatus;
            entry.ResponseBody = responseBody;
        }

        return Task.CompletedTask;
    }

    public Task DiscardAsync(string key, CancellationToken cancellationToken)
    {
        if (_entries.TryGetValue(key, out var entry))
        {
            entry.Status = "FAILED";
        }

        return Task.CompletedTask;
    }
}
