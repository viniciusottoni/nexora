using Nexora.Domain.Platform;

namespace Nexora.Contracts.Platform;

/// <summary>
/// Formato de fio (wire) das chaves/status do roteiro de implantação (US-141 §7) — mapeamento
/// EXPLÍCITO entre <see cref="OnboardingStepKey"/>/<see cref="OnboardingStepStatus"/> (Domain) e as
/// strings <c>SCREAMING_SNAKE_CASE</c> exatas do contrato de API (<c>"TENANT_CREATED"</c>,
/// <c>"EDGE_INSTALL"</c>, <c>"PAYMENT_CONFIG"</c>, ...).
/// </summary>
/// <remarks>
/// [DECISÃO DE ARQUITETURA] <c>OnboardingStepConfiguration.cs</c> (Infrastructure) já usa
/// <c>HasConversion&lt;string&gt;()</c> para persistir a coluna <c>key</c> — mas a conversão padrão
/// do EF Core para enum→string usa o NOME do membro C# (<c>Enum.ToString()</c>), então
/// <see cref="OnboardingStepKey.EdgeInstall"/> vira a string de banco <c>"EdgeInstall"</c>, não
/// <c>"EDGE_INSTALL"</c>. Isso é aceitável — a coluna do banco é um detalhe de persistência interno,
/// nunca visto pelo cliente HTTP. O contrato de API (US-141 §7, Gherkin e exemplo JSON) exige
/// exatamente <c>SCREAMING_SNAKE_CASE</c>; esta classe é o único ponto de tradução entre os dois
/// mundos — NUNCA renomear os valores do enum em <c>Nexora.Domain.Platform.OnboardingStepKey</c>
/// para "consertar" isso (quebraria a coluna já persistida e o <c>uq_onboarding_step_tenant_key</c>
/// sem uma migration dedicada, fora do escopo desta tarefa).
/// </remarks>
public static class OnboardingStepKeyWireFormat
{
    private static readonly Dictionary<OnboardingStepKey, string> ToWire = new Dictionary<OnboardingStepKey, string>
    {
        [OnboardingStepKey.TenantCreated] = "TENANT_CREATED",
        [OnboardingStepKey.Branding] = "BRANDING",
        [OnboardingStepKey.Menu] = "MENU",
        [OnboardingStepKey.Tables] = "TABLES",
        [OnboardingStepKey.EdgeInstall] = "EDGE_INSTALL",
        [OnboardingStepKey.PaymentConfig] = "PAYMENT_CONFIG",
        [OnboardingStepKey.Training] = "TRAINING",
        [OnboardingStepKey.Pilot] = "PILOT",
        [OnboardingStepKey.Activation] = "ACTIVATION",
    };

    private static readonly Dictionary<string, OnboardingStepKey> FromWire =
        ToWire.ToDictionary(kv => kv.Value, kv => kv.Key, StringComparer.Ordinal);

    private static readonly Dictionary<OnboardingStepStatus, string> StatusToWire = new Dictionary<OnboardingStepStatus, string>
    {
        [OnboardingStepStatus.Pending] = "PENDING",
        [OnboardingStepStatus.InProgress] = "IN_PROGRESS",
        [OnboardingStepStatus.Done] = "DONE",
    };

    public static string ToWireKey(OnboardingStepKey key) => ToWire[key];

    public static string ToWireStatus(OnboardingStepStatus status) => StatusToWire[status];

    /// <summary>Usado pelo PATCH de conclusão manual (<c>{key}</c> na rota) — nunca lança, devolve <c>false</c> para chave desconhecida.</summary>
    public static bool TryParseWireKey(string? wireKey, out OnboardingStepKey key)
    {
        if (wireKey is not null && FromWire.TryGetValue(wireKey, out var parsed))
        {
            key = parsed;
            return true;
        }

        key = default;
        return false;
    }
}

/// <summary>
/// Progresso quantificado de um passo (US-141 §10: "44 de 60 produtos") — hoje só preenchido para
/// <see cref="OnboardingStepKey.Menu"/> (contagem real de <c>Product</c>, US-141 §7 exemplo).
/// <see cref="Expected"/> fica nulo: não existe hoje nenhuma fonte confiável de "quantos produtos
/// este tenant deveria ter" (nenhum campo de meta/alvo em <c>TenantConfig</c> ou no template de
/// provisionamento) — inventar um heurístico fixo seria pior que omitir (US-141 é clara: "quando
/// possível"). Passo seguinte natural é a US-142 (modelos por tipo de negócio) alimentar essa meta.
/// </summary>
public sealed record OnboardingStepProgressResponse(int? Products, int? Expected);

/// <summary>Um passo do checklist (US-141 §7) — <see cref="Key"/>/<see cref="Status"/> já em formato de fio (ver <see cref="OnboardingStepKeyWireFormat"/>).</summary>
public sealed record OnboardingStepResponse(string Key, string Status, OnboardingStepProgressResponse? Progress);

/// <summary>Porta de <c>GET /v1/platform/tenants/{id}/onboarding</c> (US-141 §7).</summary>
public sealed record OnboardingStatusResponse(
    IReadOnlyList<OnboardingStepResponse> Steps,
    DateTimeOffset? StartedAt,
    int? ElapsedBusinessDays);
