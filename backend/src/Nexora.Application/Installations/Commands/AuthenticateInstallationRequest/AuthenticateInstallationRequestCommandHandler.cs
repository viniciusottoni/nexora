using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Installations.Commands.AuthenticateInstallationRequest;

/// <summary>
/// Porta fiel de <c>InstallationAuthGuard.canActivate</c>: janela de 300s no timestamp,
/// verificação de assinatura Ed25519 sobre <c>{método}\n{caminho}\n{timestamp}\n{nonce}</c> e
/// consumo de nonce anti-replay (<see cref="InstallationNonce"/>). Todas as falhas colapsam no
/// mesmo código de erro genérico — mesma decisão de segurança do guard original
/// (<c>InvalidCredentialsError</c> único, para não revelar qual etapa falhou).
/// </summary>
internal sealed class AuthenticateInstallationRequestCommandHandler
    : IRequestHandler<AuthenticateInstallationRequestCommand, Result<InstallationAuthContext>>
{
    private const int TimestampWindowSeconds = 300;

    private readonly IApplicationDbContext _db;
    private readonly IInstallationSignatureVerifier _verifier;

    public AuthenticateInstallationRequestCommandHandler(IApplicationDbContext db, IInstallationSignatureVerifier verifier)
    {
        _db = db;
        _verifier = verifier;
    }

    public async Task<Result<InstallationAuthContext>> Handle(
        AuthenticateInstallationRequestCommand request, CancellationToken cancellationToken)
    {
        if (!long.TryParse(request.Timestamp, out var unixSeconds))
        {
            return Denied();
        }

        var now = DateTimeOffset.UtcNow;
        var instant = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
        if (Math.Abs((now - instant).TotalSeconds) > TimestampWindowSeconds)
        {
            return Denied();
        }

        var installation = await _db.EdgeInstallations
            .FirstOrDefaultAsync(e => e.Id == request.InstallationId, cancellationToken);

        if (installation is null || !installation.IsInstalled || installation.PublicKey is null)
        {
            return Denied();
        }

        var message = $"{request.HttpMethod}\n{request.RequestPath}\n{request.Timestamp}\n{request.Nonce}";
        if (!_verifier.Verify(message, installation.PublicKey, request.Signature))
        {
            return Denied();
        }

        var alreadyUsed = await _db.InstallationNonces.AsNoTracking()
            .AnyAsync(n => n.InstallationId == installation.Id && n.Nonce == request.Nonce, cancellationToken);

        if (alreadyUsed)
        {
            return Denied();
        }

        _db.InstallationNonces.Add(InstallationNonce.Create(
            installation.Id,
            installation.TenantId,
            request.Nonce,
            expiresAt: now.AddSeconds(TimestampWindowSeconds)));

        // SaveChangesAsync é feito pelo TransactionBehavior (commands) — o nonce só fica
        // gravado se o restante da requisição também for bem-sucedido, exatamente como no
        // guard original (consume_installation_nonce roda antes do handler, mas dentro da
        // mesma unidade lógica de autenticação).

        return Result<InstallationAuthContext>.Success(new InstallationAuthContext(
            installation.TenantId, installation.StoreId, installation.Id));
    }

    private static Result<InstallationAuthContext> Denied() =>
        Result<InstallationAuthContext>.Failure(
            "Não foi possível autenticar a requisição da instalação.",
            ApiErrorCodes.InstallationSignatureInvalidCredentials);
}
