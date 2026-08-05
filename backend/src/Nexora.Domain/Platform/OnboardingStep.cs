using Nexora.Domain.Common;

namespace Nexora.Domain.Platform;

/// <summary>Chave de cada um dos nove passos do roteiro de implantação (US-141 §3.1, Visão Geral 8.5).</summary>
public enum OnboardingStepKey
{
    TenantCreated = 0,
    Branding = 1,
    Menu = 2,
    Tables = 3,
    EdgeInstall = 4,
    PaymentConfig = 5,
    Training = 6,
    Pilot = 7,
    Activation = 8
}

public enum OnboardingStepStatus
{
    Pending = 0,
    InProgress = 1,
    Done = 2
}

/// <summary>
/// Progresso de um passo do checklist de implantação de um tenant (US-141) — torna os nove passos
/// da Visão Geral (8.5) rastreáveis e visíveis dos dois lados (Replay e cliente), em vez de
/// depender de troca de mensagens para saber "o que falta". Uma linha por
/// (<see cref="TenantId"/>, <see cref="Key"/>), semeada inteira na criação do tenant
/// (<see cref="SeedAll"/>) com <see cref="OnboardingStepKey.TenantCreated"/> já concluído.
/// </summary>
public sealed class OnboardingStep
{
    private OnboardingStep() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public OnboardingStepKey Key { get; private set; }
    public OnboardingStepStatus Status { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public Guid? CompletedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static OnboardingStep Create(Guid tenantId, OnboardingStepKey key, DateTimeOffset now)
    {
        var isTenantCreated = key == OnboardingStepKey.TenantCreated;

        return new OnboardingStep
        {
            Id = IdGenerator.NewId(),
            TenantId = tenantId,
            Key = key,
            Status = isTenantCreated ? OnboardingStepStatus.Done : OnboardingStepStatus.Pending,
            CompletedAt = isTenantCreated ? now : null,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    /// <summary>Semeia os nove passos de uma vez, na criação do tenant (RF-PLT-05).</summary>
    public static IReadOnlyList<OnboardingStep> SeedAll(Guid tenantId, DateTimeOffset now) =>
        Enum.GetValues<OnboardingStepKey>().Select(key => Create(tenantId, key, now)).ToList();

    public void Start(DateTimeOffset at)
    {
        if (Status == OnboardingStepStatus.Done)
            return;

        Status = OnboardingStepStatus.InProgress;
        UpdatedAt = at;
    }

    public void Complete(DateTimeOffset at, Guid? completedBy)
    {
        Status = OnboardingStepStatus.Done;
        CompletedAt = at;
        CompletedBy = completedBy;
        UpdatedAt = at;
    }
}
