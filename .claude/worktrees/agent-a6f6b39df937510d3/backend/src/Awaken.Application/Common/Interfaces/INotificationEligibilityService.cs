namespace Awaken.Application.Common.Interfaces;

public record EligibilityResult(bool Allowed, string? BlockReason)
{
    public static EligibilityResult Allow() => new(true, null);
    public static EligibilityResult Blocked(string reason) => new(false, reason);
}

public interface INotificationEligibilityService
{
    /// US-095: avalia se o usuário pode receber uma notificação do tipo informado.
    /// Verifica consentimento, acesso, redundância, limite diário e prioridade.
    /// NÃO persiste o resultado — responsabilidade do chamador.
    Task<EligibilityResult> EvaluateAsync(
        Guid userId,
        string notificationType,
        DateTime utcNow,
        CancellationToken cancellationToken = default);
}
