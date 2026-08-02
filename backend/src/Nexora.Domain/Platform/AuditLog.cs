using Nexora.Domain.Common;

namespace Nexora.Domain.Platform;

/// <summary>
/// Registro de auditoria — append-only (trigger de bloqueio de UPDATE/DELETE, documento 10).
/// Registro de fato, sem regra de negócio própria além de existir.
/// </summary>
public sealed class AuditLog
{
    private AuditLog() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid? StoreId { get; private set; }
    public Guid? ActorId { get; private set; }
    public Guid? AuthorizedBy { get; private set; }
    public Guid? DeviceId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string Entity { get; private set; } = string.Empty;
    public Guid? EntityId { get; private set; }

    // TODO: value object tipado quando o formato de before for definido — hoje é JSONB livre
    public string? Before { get; private set; }

    // TODO: value object tipado quando o formato de after for definido — hoje é JSONB livre
    public string? After { get; private set; }

    public string? Reason { get; private set; }

    // Mapeado como INET no PostgreSQL; mantido como string na camada de domínio para não
    // acoplar Domain a System.Net além do estritamente necessário.
    public string? Ip { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }

    public Tenant Tenant { get; private set; } = null!;

    public static AuditLog Create(
        Guid tenantId,
        string action,
        string entity,
        DateTimeOffset occurredAt,
        Guid? storeId = null,
        Guid? actorId = null,
        Guid? authorizedBy = null,
        Guid? deviceId = null,
        Guid? entityId = null,
        string? before = null,
        string? after = null,
        string? reason = null,
        string? ip = null)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new DomainException("A ação auditada é obrigatória.");

        if (string.IsNullOrWhiteSpace(entity))
            throw new DomainException("A entidade auditada é obrigatória.");

        return new AuditLog
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            StoreId = storeId,
            ActorId = actorId,
            AuthorizedBy = authorizedBy,
            DeviceId = deviceId,
            Action = action,
            Entity = entity,
            EntityId = entityId,
            Before = before,
            After = after,
            Reason = reason,
            Ip = ip,
            OccurredAt = occurredAt,
            RecordedAt = DateTimeOffset.UtcNow
        };
    }
}
