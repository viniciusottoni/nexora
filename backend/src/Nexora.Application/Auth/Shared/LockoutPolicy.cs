namespace Nexora.Application.Auth.Shared;

/// <summary>
/// Bloqueio por excesso de tentativas — porta de packages/domain/src/auth/lockout.ts
/// (registerFailedPinAttempt/retryAfterSeconds). Usado tanto pelo login por PIN (bloqueio por
/// dispositivo, <c>AuthAttempt</c>) quanto pelo login por senha (bloqueio por usuário,
/// <c>AppUser.FailedAttempts</c>/<c>BlockedUntil</c>) — mesma política, dois agregados diferentes.
/// </summary>
internal static class LockoutPolicy
{
    public const int MaxFailedAttempts = 5;
    public const int LockMinutes = 15;

    /// <summary>Segundos restantes até o bloqueio expirar (0 se não houver bloqueio ativo) — porta de retryAfterSeconds.</summary>
    public static int RetryAfterSeconds(DateTimeOffset? blockedUntil, DateTimeOffset now)
    {
        if (blockedUntil is null)
        {
            return 0;
        }

        var seconds = (int)Math.Ceiling((blockedUntil.Value - now).TotalSeconds);
        return Math.Max(0, seconds);
    }
}
