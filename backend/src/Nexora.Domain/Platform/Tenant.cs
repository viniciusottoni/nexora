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
    public TenantStatus Status { get; private set; } = TenantStatus.Provisioned;

    /// <summary>
    /// Controle de concorrência otimista da máquina de estados (US-153 §7, header <c>If-Match</c>) —
    /// incrementado a cada <see cref="TransitionStatus"/> bem-sucedida. Começa em 1 (não 0) para que
    /// o sentinela de <c>If-Match</c> ausente/malformado (0) nunca combine por acidente com um tenant
    /// real.
    /// </summary>
    public int StatusVersion { get; private set; } = 1;
    public string Plan { get; private set; } = "STANDARD";

    /// <summary>
    /// US-154 · Gestão de planos e configuração comercial — controle de concorrência otimista
    /// (header <c>If-Match</c> de <c>PUT /v1/platform/tenants/{id}/plan</c>) próprio do plano
    /// comercial, independente de <see cref="StatusVersion"/> (ciclo de vida operacional, US-153) —
    /// são dimensões diferentes do tenant (doc §15 "Não misturar 'plano comercial' com 'estado da
    /// instalação'"). Incrementado em <see cref="SchedulePlanChange"/> (a cada intenção confirmada
    /// pelo administrador, mesmo quando a vigência é futura), nunca em <see cref="ApplyScheduledPlan"/>
    /// (efetivação tardia não é uma nova decisão administrativa, é só o relógio alcançando uma
    /// decisão já tomada). Começa em 1 pelo mesmo motivo de <see cref="StatusVersion"/>.
    /// </summary>
    public int PlanVersion { get; private set; } = 1;
    public string Timezone { get; private set; } = "America/Sao_Paulo";
    public string Locale { get; private set; } = "pt-BR";
    public string Currency { get; private set; } = "BRL";
    public string? Domain { get; private set; }

    /// <summary>
    /// Espelha o e-mail do proprietário atual (US-151, diretório de estabelecimentos) —
    /// denormalizado na única tabela sem RLS pelo mesmo motivo de <see cref="Plan"/>: o diretório
    /// (P9) precisa buscar por e-mail do dono cruzando TODOS os tenants, e <c>app_user</c> tem RLS
    /// (ADR-004, "falha fechada" sem <c>app.tenant_id</c> no contexto). Espelhar aqui evita
    /// introduzir o papel <c>platform_admin</c> (BYPASSRLS) — decisão de arquitetura maior, fora do
    /// escopo desta US (ver nota em <c>EmailOutboxDeliveryWorker</c>) — ou uma view materializada
    /// nova (US-151 §15 deixa essa escolha em aberto, "medir antes de escolher"). Definido no
    /// provisionamento (<c>ProvisionTenantCommandHandler</c>), a partir do e-mail do proprietário.
    /// </summary>
    public string? OwnerEmail { get; private set; }

    /// <summary>
    /// Espelha o código do modelo de negócio aplicado no provisionamento (US-151) — mesma razão de
    /// <see cref="OwnerEmail"/>: o diretório de plataforma filtra por modelo sem depender de RLS por
    /// tenant. Não é o "Plan" comercial (US-154): é o template operacional (<c>business_template</c>,
    /// docs/domain/12), já espelhado hoje em <c>tenant_config.template_code</c> (US-142) mas
    /// inacessível ao diretório pelo mesmo motivo RLS de <see cref="OwnerEmail"/>.
    /// </summary>
    public string? TemplateCode { get; private set; }

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
            Status = TenantStatus.Provisioned,
            StatusVersion = 1,
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

    /// <summary>
    /// Define/atualiza <see cref="OwnerEmail"/> (US-151) — chamado pelo provisionamento logo após
    /// <see cref="Create"/>, a partir do e-mail informado para o proprietário.
    /// </summary>
    public void SetOwnerEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("O e-mail do proprietário é obrigatório.");

        OwnerEmail = email.Trim().ToLowerInvariant();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Define/atualiza <see cref="TemplateCode"/> (US-151) — chamado pelo provisionamento assim que
    /// o <c>business_template</c> aplicado é conhecido.
    /// </summary>
    public void SetTemplateCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("O código do modelo de negócio é obrigatório.");

        TemplateCode = code.Trim().ToUpperInvariant();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// US-154 · Gestão de planos e configuração comercial — atribui o plano no PROVISIONAMENTO
    /// (sem histórico: não há "plano anterior" real, é a primeira atribuição). Corrige o bug em que
    /// <see cref="Create"/> sempre persistia o literal <c>"STANDARD"</c> independente do que o
    /// formulário enviou (US-154 §4, cenário "Provisionamento preserva o plano solicitado") — o
    /// chamador (<c>ProvisionTenantCommandHandler</c>) só invoca isto depois de validar o código
    /// contra o catálogo ativo (<see cref="PlatformPlan"/>), então o valor aqui é sempre o código
    /// JÁ confirmado, nunca substituído silenciosamente por um default.
    /// </summary>
    public void SetPlan(string plan)
    {
        if (string.IsNullOrWhiteSpace(plan))
            throw new DomainException("O plano do tenant é obrigatório.");

        Plan = plan.Trim().ToUpperInvariant();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// US-154 · Gestão de planos e configuração comercial — agenda uma mudança de plano comercial
    /// (upgrade/downgrade) com vigência. Quando <paramref name="effectiveAt"/> já passou (ou é
    /// agora), o novo plano entra em vigor IMEDIATAMENTE, na mesma chamada — mesmo espírito de
    /// <see cref="TransitionStatus"/> (que também aplica e retorna o estado anterior numa única
    /// operação). Quando a vigência é futura, <see cref="Plan"/> permanece o atual até que
    /// <see cref="ApplyScheduledPlan"/> seja chamado mais tarde (efetivação, síncrona ou preguiçosa
    /// — ver <c>GetTenantPlanQueryHandler</c>) — o cenário "Mudança de plano com vigência" exige
    /// explicitamente que "o plano atual deve permanecer até a vigência".
    /// </summary>
    /// <returns>O plano anterior (para montar o histórico/evento) e se a mudança já foi aplicada nesta chamada.</returns>
    public (string Previous, bool AppliedImmediately) SchedulePlanChange(string plan, DateTimeOffset effectiveAt, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(plan))
            throw new DomainException("O plano do tenant é obrigatório.");

        var previous = Plan;
        var normalized = plan.Trim().ToUpperInvariant();
        PlanVersion++;
        UpdatedAt = now;

        if (effectiveAt > now)
        {
            return (previous, false);
        }

        Plan = normalized;
        return (previous, true);
    }

    /// <summary>
    /// US-154 · Gestão de planos e configuração comercial — efetiva uma mudança de plano
    /// PREVIAMENTE agendada (<see cref="SchedulePlanChange"/> com vigência futura) cuja data já
    /// chegou. Não incrementa <see cref="PlanVersion"/> de novo (ver docstring da propriedade): a
    /// decisão administrativa já foi contada no agendamento, isto só materializa o relógio
    /// alcançando-a.
    /// </summary>
    /// <returns>O plano anterior (para o evento <c>tenant.plan_changed</c>/EVT-057).</returns>
    public string ApplyScheduledPlan(string plan, DateTimeOffset at)
    {
        if (string.IsNullOrWhiteSpace(plan))
            throw new DomainException("O plano do tenant é obrigatório.");

        var previous = Plan;
        Plan = plan.Trim().ToUpperInvariant();
        UpdatedAt = at;
        return previous;
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

    /// <summary>
    /// US-153 · Ciclo de vida do estabelecimento — transição de status validada pela máquina de
    /// estados canônica (<see cref="TenantStatusTransitions"/>), com controle de concorrência
    /// otimista via <see cref="StatusVersion"/>. Diferente de <see cref="Activate"/>/<see cref="Suspend"/>/
    /// <see cref="Cancel"/> (setters simples pré-existentes, mantidos só para não quebrar fixtures de
    /// teste que montam estado direto): este é o único caminho usado pelos comandos reais
    /// (<c>TransitionTenantStatusCommand</c>, <c>ActivateTenantCommand</c>,
    /// <c>RegisterInstallationCommandHandler</c>), o que garante "nenhuma transição sem evento"
    /// (ADR-006) em todas as três origens. Lança <see cref="DomainException"/> em transição inválida
    /// — a Application pré-valida com a MESMA matriz (<see cref="TenantStatusTransitions.IsValid"/>)
    /// para devolver 409 TENANT_STATUS_TRANSITION_INVALID em vez de deixar a exceção virar 500.
    /// </summary>
    /// <returns>O status anterior à transição (para montar o histórico/evento).</returns>
    public TenantStatus TransitionStatus(TenantStatus target, DateTimeOffset at)
    {
        if (!TenantStatusTransitions.IsValid(Status, target))
        {
            throw new DomainException($"Transição de status de {Status} para {target} não é permitida.");
        }

        var previous = Status;
        Status = target;
        StatusVersion++;
        UpdatedAt = at;

        if (target == TenantStatus.Active && ActivatedAt is null)
        {
            ActivatedAt = at;
        }

        return previous;
    }

    public void SoftDelete()
    {
        DeletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
