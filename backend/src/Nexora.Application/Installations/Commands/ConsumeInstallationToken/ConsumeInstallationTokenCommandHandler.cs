using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Installations.Abstractions;
using Nexora.Application.Installations.Support;
using Nexora.Contracts.Installations;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Nexora.Application.Installations.Commands.ConsumeInstallationToken;

/// <summary>
/// Limitação conhecida em relação ao original: o teste TS <c>consume-installation-token.service.test.ts</c>
/// espera que o reuso do token grave auditoria (<c>EDGE_INSTALL_TOKEN_REUSED</c>) mesmo quando a
/// operação é rejeitada. Aqui isso não é possível sem alterar o <c>TransactionBehavior</c>
/// compartilhado (ele só chama <c>SaveChangesAsync</c> quando o <see cref="Result"/> é sucesso —
/// ver ADR-037/TransactionBehavior.cs) — mudar esse comportamento é decisão de arquitetura maior
/// que este módulo. Documentado aqui para quem revisar decidir se vale um ADR de "auditoria em
/// falha" com um behavior dedicado.
/// </summary>
internal sealed class ConsumeInstallationTokenCommandHandler
    : IRequestHandler<ConsumeInstallationTokenCommand, Result<ConsumeInstallationTokenResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IInstallationTokenDigester _tokenDigester;
    private readonly ILogger<ConsumeInstallationTokenCommandHandler> _logger;

    public ConsumeInstallationTokenCommandHandler(
        IApplicationDbContext db, IInstallationTokenDigester tokenDigester, ILogger<ConsumeInstallationTokenCommandHandler> logger)
    {
        _db = db;
        _tokenDigester = tokenDigester;
        _logger = logger;
    }

    public async Task<Result<ConsumeInstallationTokenResponse>> Handle(
        ConsumeInstallationTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = _tokenDigester.Digest(request.RawToken);

        var installation = await PlatformInstallationLookup.FindByTokenHashAsync(
            _db, tokenHash, cancellationToken);

        if (installation is null)
        {
            return Result<ConsumeInstallationTokenResponse>.Failure(
                "Token de instalação inválido.", ApiErrorCodes.InstallationNotFound);
        }

        if (installation.IsTokenExpired(DateTimeOffset.UtcNow))
        {
            return Result<ConsumeInstallationTokenResponse>.Failure(
                "O token de instalação expirou.", ApiErrorCodes.InstallationTokenExpired);
        }

        if (installation.IsTokenConsumed)
        {
            _logger.LogWarning(
                "EDGE_INSTALL_TOKEN_REUSED: InstallationId={InstallationId}", installation.Id);

            return Result<ConsumeInstallationTokenResponse>.Failure(
                "Este token de instalação já foi utilizado.", ApiErrorCodes.InstallationTokenConflict);
        }

        installation.ReserveInstallToken();

        // SaveChangesAsync é feito pelo TransactionBehavior (commands).

        return Result<ConsumeInstallationTokenResponse>.Success(new ConsumeInstallationTokenResponse(
            installation.TenantId,
            installation.StoreId,
            installation.Id,
            installation.TokenConsumedAt!.Value));
    }
}
