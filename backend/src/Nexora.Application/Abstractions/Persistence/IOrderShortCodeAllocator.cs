namespace Nexora.Application.Abstractions.Persistence;

/// <summary>
/// Aloca o próximo <c>short_code</c> único (ADR-016) para um pedido novo, dentro da transação
/// corrente (a mesma aberta por <c>TransactionBehavior</c> ao redor do handler). US-030 §7 exige
/// "retentativa em colisão, nunca estourar exceção pro cliente" — mas a detecção de violação de
/// unicidade/lock consultivo do Postgres só pode viver em <c>Nexora.Infrastructure</c> (ADR-039
/// proíbe <c>Nexora.Application</c> de referenciar Npgsql/SQL cru). Implementado sobre
/// <c>pg_advisory_xact_lock</c>, escopado a <c>(storeId, businessDay)</c> e liberado automaticamente
/// no fim da transação — serializa concorrência sem depender de retry por exceção.
/// </summary>
public interface IOrderShortCodeAllocator
{
    Task<string> AllocateAsync(Guid storeId, DateOnly businessDay, CancellationToken cancellationToken);
}
