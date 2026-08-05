using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Installations.Support;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Nexora.Application.Installations.Commands.RevokeInstallationCredential;

/// <summary>
/// US-156 · Recuperação do provisionamento e token de instalação — "revogação manual de token
/// comprometido" (fora dos cenários Gherkin de rotação automática, mas explicitamente pedido no
/// escopo da história). Idempotente: revogar uma credencial já revogada não é erro (mesma semântica
/// de DELETE do resto da API — RN-004 exige o registro em <c>audit_log</c> mesmo assim, já que é
/// uma AÇÃO administrativa distinta, ainda que o estado final não mude).
/// </summary>
internal sealed class RevokeInstallationCredentialCommandHandler
    : IRequestHandler<RevokeInstallationCredentialCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ILogger<RevokeInstallationCredentialCommandHandler> _logger;

    public RevokeInstallationCredentialCommandHandler(
        IApplicationDbContext db, ILogger<RevokeInstallationCredentialCommandHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result> Handle(RevokeInstallationCredentialCommand request, CancellationToken cancellationToken)
    {
        var installation = await PlatformInstallationLookup.FindAsync(_db, request.InstallationId, cancellationToken);
        if (installation is null)
        {
            return Result.Failure("Instalação não encontrada.", ApiErrorCodes.InstallationNotFound);
        }

        await _db.SetTenantContextAsync(installation.TenantId, cancellationToken);

        var credential = await _db.InstallationCredentials
            .FirstOrDefaultAsync(
                c => c.Id == request.CredentialId && c.InstallationId == request.InstallationId, cancellationToken);

        if (credential is null)
        {
            return Result.Failure(
                "Credencial de instalação não encontrada.", ApiErrorCodes.InstallationCredentialNotFound);
        }

        var now = DateTimeOffset.UtcNow;
        var wasAlreadyRevoked = credential.RevokedAt is not null;
        credential.Revoke(now);

        // RN-015: o token vinculado à instalação ("legado", campos de EdgeInstallation) só é
        // invalidado se a credencial revogada FOR a que esses campos ainda apontam — comparar por
        // hash, nunca pelo segredo bruto (que este handler nunca vê).
        if (!wasAlreadyRevoked &&
            installation.InstallTokenHash == credential.TokenHash &&
            !installation.IsTokenConsumed)
        {
            installation.InvalidateInstallToken();
        }

        _db.AuditLogs.Add(AuditLog.Create(
            installation.TenantId,
            action: "INSTALLATION_CREDENTIAL_REVOKED",
            entity: nameof(InstallationCredential),
            occurredAt: now,
            storeId: installation.StoreId,
            actorId: request.ActorId,
            entityId: credential.Id,
            // RN-004: nunca o segredo bruto nem o hash.
            after: JsonSerializer.Serialize(new { credentialId = credential.Id, alreadyRevoked = wasAlreadyRevoked }),
            reason: request.Reason));

        _logger.LogInformation(
            "Credencial de instalação revogada. InstallationId={InstallationId}, CredentialId={CredentialId}",
            installation.Id, credential.Id);

        return Result.Success();
    }
}
