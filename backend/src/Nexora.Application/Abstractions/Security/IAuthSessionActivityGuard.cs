using Nexora.Application.Abstractions.Messaging;

namespace Nexora.Application.Abstractions.Security;

/// <summary>
/// Encerra sessão operacional sem atividade recente (US-004, gap "encerramento de sessão inativa
/// configurável" — 100% não implementado antes desta correção). Chamado uma vez por requisição
/// autenticada — <c>Api.Edge</c>/<c>Api.Cloud</c> plugam isto num middleware leve (depois de
/// <c>UseAuthentication()</c>) para não duplicar a regra de negócio em cada projeto de Api: só o
/// fio (ler claim, escrever 401) muda entre edge/cloud, a decisão em si é a mesma nos dois.
/// </summary>
public interface IAuthSessionActivityGuard
{
    /// <summary>
    /// Nega (<see cref="Result.IsFailure"/>, código <c>AUTH_SESSION_IDLE_TIMEOUT</c>) quando a
    /// sessão não existe, já foi revogada, ou está inativa há mais tempo que
    /// <c>TenantConfig.Operation.sessionInactivityMinutes</c> (padrão 30 min —
    /// <see cref="Nexora.Application.Auth.Shared.SessionInactivityPolicy.DefaultMinutes"/>).
    /// Em sucesso, já atualizou <c>AuthSession.LastActiveAt</c> e persistiu (chamador não precisa
    /// dar <c>SaveChangesAsync</c> de novo).
    /// </summary>
    Task<Result> EnforceAsync(Guid tenantId, Guid sessionId, CancellationToken cancellationToken = default);
}
