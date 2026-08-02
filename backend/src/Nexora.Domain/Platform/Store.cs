using Nexora.Domain.Common;

namespace Nexora.Domain.Platform;

/// <summary>
/// Loja — unidade física de operação. Preparado para rede multi-unidade; na Fase 1 todo
/// tenant tem uma única loja marcada como padrão (<see cref="IsDefault"/>).
/// </summary>
public sealed class Store
{
    private readonly List<Device> _devices = new();
    private readonly List<EdgeInstallation> _edgeInstallations = new();
    private readonly List<Station> _stations = new();

    private Store() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Timezone { get; private set; } = "America/Sao_Paulo";

    // TODO: value object tipado quando o formato de address for definido — hoje é JSONB livre
    public string? Address { get; private set; }

    public string? Phone { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public IReadOnlyCollection<Device> Devices => _devices.AsReadOnly();
    public IReadOnlyCollection<EdgeInstallation> EdgeInstallations => _edgeInstallations.AsReadOnly();
    public IReadOnlyCollection<Station> Stations => _stations.AsReadOnly();

    public static Store Create(Guid tenantId, string name, bool isDefault = false, string timezone = "America/Sao_Paulo")
        => Create(IdGenerator.NewId(), tenantId, name, isDefault, timezone);

    /// <summary>
    /// Variante com id explícito — usada pela importação do bootstrap do edge (ADR-019), onde
    /// a loja já existe na nuvem e o id precisa ser preservado na primeira carga local.
    /// </summary>
    public static Store Create(Guid id, Guid tenantId, string name, bool isDefault = false, string timezone = "America/Sao_Paulo")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("O nome da loja é obrigatório.");

        var now = DateTimeOffset.UtcNow;

        return new Store
        {
            Id = id,
            TenantId = tenantId,
            Name = name,
            Timezone = timezone,
            IsDefault = isDefault,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>Atualiza nome/fuso a partir de uma carga de bootstrap recebida da nuvem (upsert idempotente).</summary>
    public void ApplyBootstrapIdentity(string name, string timezone)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("O nome da loja é obrigatório.");

        Name = name;
        Timezone = timezone;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsDefault()
    {
        IsDefault = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UnmarkAsDefault()
    {
        IsDefault = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SoftDelete()
    {
        DeletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
