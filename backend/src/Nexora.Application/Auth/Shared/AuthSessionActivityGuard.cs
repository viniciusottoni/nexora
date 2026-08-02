using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Shared.Errors;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Auth.Shared;

/// <summary>
/// Implementação de <see cref="IAuthSessionActivityGuard"/> — porta direta sobre
/// <see cref="IApplicationDbContext"/>, sem nenhuma dependência de ASP.NET Core (por isso vive em
/// Application, não em Api.Edge/Api.Cloud, apesar de ser chamada a cada requisição HTTP: a decisão
/// de negócio "esta sessão ainda está viva?" não deveria mudar entre edge e cloud, só o middleware
/// fino que a invoca). Testável direto contra Postgres real via <c>IApplicationDbContext</c>, sem
/// precisar de host HTTP nenhum.
/// </summary>
public sealed class AuthSessionActivityGuard : IAuthSessionActivityGuard
{
    private readonly IApplicationDbContext _db;

    public AuthSessionActivityGuard(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result> EnforceAsync(Guid tenantId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _db.AuthSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.TenantId == tenantId, cancellationToken);

        if (session is null || session.IsRevoked)
        {
            return Deny();
        }

        var now = DateTimeOffset.UtcNow;
        var config = await _db.TenantConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.TenantId == tenantId, cancellationToken);
        var idleTimeout = TimeSpan.FromMinutes(SessionInactivityPolicy.ResolveMinutes(config?.Operation));

        if (now - session.LastActiveAt > idleTimeout)
        {
            return Deny();
        }

        session.RecordActivity();
        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static Result Deny() =>
        Result.Failure(AuthErrorMessages.SessionIdleTimeout, ApiErrorCodes.AuthSessionIdleTimeout);
}
