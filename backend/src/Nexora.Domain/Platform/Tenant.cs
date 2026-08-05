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

    /// <summary>Início do roteiro de implantação (US-141) — marcado na criação do tenant, base da métrica de tempo de implantação.</summary>
    public DateTimeOffset? OnboardingStartedAt { get; private set; }

    /// <summary>Instante de ativação (US-141) — fim da medição "da criação do tenant à ativação".</summary>
    public DateTimeOffset? ActivatedAt { get; private set; }
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
            OnboardingStartedAt = now,
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

    /// <summary>
    /// Ativação ao final do roteiro de implantação (US-141) — distinta de <see cref="Activate"/>
    /// (que só muda o status): aqui também se fecha a medição de tempo de implantação
    /// (<see cref="OnboardingStartedAt"/> → <see cref="ActivatedAt"/>, meta ≤ 5 dias úteis).
    /// </summary>
    public void CompleteOnboarding(DateTimeOffset at)
    {
        Status = TenantStatus.Active;
        ActivatedAt = at;
        UpdatedAt = at;
    }

    /// <summary>
    /// Espelha o domínio próprio primário e ativo do tenant (US-143) — mantém
    /// <see cref="Domain"/> como o único valor consultado pela resolução de tenant por host em
    /// tempo de requisição (<c>GetPublicBrandingQueryHandler</c> e afins, mecanismo herdado da
    /// US-003), mesmo depois de <c>tenant_domain</c> (US-143) existir. Decisão de projeto: não foi
    /// possível consultar <c>tenant_domain</c> diretamente nesses handlers públicos/anônimos porque
    /// a tabela tem RLS (ADR-004, "falha fechada" — sem <c>app.tenant_id</c> no contexto, RETORNA
    /// ZERO linhas, mesmo para leitura), e esses handlers rodam exatamente ANTES de qualquer tenant
    /// ser conhecido. Espelhar o domínio verificado aqui, na tabela <c>tenant</c> (a única sem RLS,
    /// "raiz global"), preserva o caminho de leitura rápido e sem varredura — chamado por
    /// <c>VerifyTenantDomainCommandHandler</c> só quando o domínio verificado é o primeiro/primário
    /// do tenant (<c>TenantDomain.IsPrimary</c>).
    /// </summary>
    public void SetCustomDomain(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
            throw new DomainException("O domínio é obrigatório.");

        Domain = domain.Trim().ToLowerInvariant();
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
