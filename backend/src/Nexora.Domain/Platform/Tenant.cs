using Nexora.Domain.Common;

namespace Nexora.Domain.Platform;

/// <summary>
/// Estabelecimento — unidade de isolamento multi-tenant (ADR-004). Raiz da hierarquia:
/// é a única tabela de negócio sem tenant_id e sem RLS.
/// </summary>
public sealed class Tenant
{
    private readonly List<Store> _stores = new();
    private readonly List<AppUser> _users = new();
    private readonly List<Role> _roles = new();
    private readonly List<Device> _devices = new();
    private readonly List<EdgeInstallation> _edgeInstallations = new();
    private readonly List<AuditLog> _auditLogs = new();

    private Tenant() { }

    public Guid Id { get; private set; }
    public string Slug { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? LegalName { get; private set; }
    public string? Document { get; private set; }
    public TenantStatus Status { get; private set; } = TenantStatus.Trial;
    public string Plan { get; private set; } = "STANDARD";
    public string Timezone { get; private set; } = "America/Sao_Paulo";
    public string Locale { get; private set; } = "pt-BR";
    public string Currency { get; private set; } = "BRL";
    public string? Domain { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public TenantConfig? Config { get; private set; }
    public IReadOnlyCollection<Store> Stores => _stores.AsReadOnly();
    public IReadOnlyCollection<AppUser> Users => _users.AsReadOnly();
    public IReadOnlyCollection<Role> Roles => _roles.AsReadOnly();
    public IReadOnlyCollection<Device> Devices => _devices.AsReadOnly();
    public IReadOnlyCollection<EdgeInstallation> EdgeInstallations => _edgeInstallations.AsReadOnly();
    public IReadOnlyCollection<AuditLog> AuditLogs => _auditLogs.AsReadOnly();

    public static Tenant Create(string slug, string name, string timezone = "America/Sao_Paulo", string locale = "pt-BR", string currency = "BRL")
        => Create(IdGenerator.NewId(), slug, name, timezone, locale, currency);

    /// <summary>
    /// Variante com id explícito — usada pela importação do bootstrap do edge (ADR-019), onde
    /// o tenant já existe na nuvem e o id precisa ser preservado na primeira carga local
    /// (identidade é decidida pela nuvem, nunca gerada de novo no edge).
    /// </summary>
    public static Tenant Create(Guid id, string slug, string name, string timezone = "America/Sao_Paulo", string locale = "pt-BR", string currency = "BRL")
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainException("O slug do tenant é obrigatório.");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("O nome do tenant é obrigatório.");

        var now = DateTimeOffset.UtcNow;

        return new Tenant
        {
            Id = id,
            Slug = slug,
            Name = name,
            Status = TenantStatus.Trial,
            Plan = "STANDARD",
            Timezone = timezone,
            Locale = locale,
            Currency = currency,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>Atualiza nome/slug a partir de uma carga de bootstrap recebida da nuvem (upsert idempotente).</summary>
    public void ApplyBootstrapIdentity(string name, string slug, string timezone)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("O nome do tenant é obrigatório.");

        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainException("O slug do tenant é obrigatório.");

        Name = name;
        Slug = slug;
        Timezone = timezone;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        Status = TenantStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Suspend()
    {
        Status = TenantStatus.Suspended;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        Status = TenantStatus.Cancelled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SoftDelete()
    {
        DeletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
