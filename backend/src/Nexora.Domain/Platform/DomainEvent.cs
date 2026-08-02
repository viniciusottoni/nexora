using Nexora.Domain.Common;

namespace Nexora.Domain.Platform;

/// <summary>
/// Evento de domínio append-only (ADR-006). Nenhuma transição de estado é gravada sem seu
/// evento correspondente, na mesma transação. Registro de fato — sem regra de negócio própria.
/// </summary>
public sealed class DomainEvent
{
    private DomainEvent() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid? StoreId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public short Version { get; private set; } = 1;
    public string AggregateType { get; private set; } = string.Empty;
    public Guid AggregateId { get; private set; }

    // TODO: value object tipado quando o formato de payload for definido — hoje é JSONB livre
    public string Payload { get; private set; } = "{}";

    public Guid? ActorId { get; private set; }
    public Guid? AuthorizedBy { get; private set; }
    public Guid? DeviceId { get; private set; }

    // 'EDGE' ou 'CLOUD' (event_origin no DDL) — mantido como string pois o schema.prisma
    // (fonte da verdade dos campos) declara o campo como String, não como enum tipado.
    public string Origin { get; private set; } = string.Empty;

    public long? DeviceSeq { get; private set; }
    public Guid? InstallationId { get; private set; }
    public string? TraceId { get; private set; }
    public bool ClockSuspect { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }

    public static DomainEvent Create(
        Guid tenantId,
        string type,
        string aggregateType,
        Guid aggregateId,
        string payload,
        string origin,
        DateTimeOffset occurredAt,
        Guid? storeId = null,
        Guid? actorId = null,
        Guid? authorizedBy = null,
        Guid? deviceId = null,
        long? deviceSeq = null,
        Guid? installationId = null,
        string? traceId = null,
        bool clockSuspect = false,
        short version = 1)
    {
        if (string.IsNullOrWhiteSpace(type))
            throw new DomainException("O tipo do evento de domínio é obrigatório.");

        if (string.IsNullOrWhiteSpace(aggregateType))
            throw new DomainException("O tipo do agregado do evento de domínio é obrigatório.");

        if (string.IsNullOrWhiteSpace(origin))
            throw new DomainException("A origem do evento de domínio é obrigatória.");

        return new DomainEvent
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            StoreId = storeId,
            Type = type,
            Version = version,
            AggregateType = aggregateType,
            AggregateId = aggregateId,
            Payload = payload,
            ActorId = actorId,
            AuthorizedBy = authorizedBy,
            DeviceId = deviceId,
            Origin = origin,
            DeviceSeq = deviceSeq,
            InstallationId = installationId,
            TraceId = traceId,
            ClockSuspect = clockSuspect,
            OccurredAt = occurredAt,
            RecordedAt = DateTimeOffset.UtcNow
        };
    }
}
