using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Notifications;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.TenantDomains.Commands.RenewTenantDomainCertificate;

/// <summary>
/// US-143 §4, cenário "Falha de renovação" — quando <see cref="ICertificateIssuer"/> falha, marca
/// <c>TenantDomain.MarkCertificateFailed</c> e notifica a plataforma
/// (<see cref="IPlatformAlertNotifier"/>). Implementa <see cref="IPersistsStateOnFailureCommand"/>:
/// a falha de renovação É o resultado de negócio esperado deste comando quando o emissor recusa —
/// precisa persistir (senão o worker reencontraria o mesmo domínio "prestes a vencer" a cada
/// varredura sem nunca registrar a tentativa) mesmo devolvendo <c>Result.Failure</c>.
/// </summary>
internal sealed class RenewTenantDomainCertificateCommandHandler
    : IRequestHandler<RenewTenantDomainCertificateCommand, Result<bool>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICertificateIssuer _certificateIssuer;
    private readonly IPlatformAlertNotifier _platformAlertNotifier;

    public RenewTenantDomainCertificateCommandHandler(
        IApplicationDbContext db, ICertificateIssuer certificateIssuer, IPlatformAlertNotifier platformAlertNotifier)
    {
        _db = db;
        _certificateIssuer = certificateIssuer;
        _platformAlertNotifier = platformAlertNotifier;
    }

    public async Task<Result<bool>> Handle(RenewTenantDomainCertificateCommand request, CancellationToken cancellationToken)
    {
        await _db.SetTenantContextAsync(request.TenantId, cancellationToken);

        var domain = await _db.TenantDomains
            .FirstOrDefaultAsync(d => d.Id == request.DomainId && d.TenantId == request.TenantId && d.DeletedAt == null, cancellationToken);

        if (domain is null)
        {
            return Result<bool>.Failure("Domínio não encontrado.", ApiErrorCodes.TenantDomainNotFound);
        }

        var issuance = await _certificateIssuer.IssueAsync(domain.Domain, cancellationToken);
        if (issuance is { Succeeded: true, IssuedAt: { } issuedAt, ExpiresAt: { } expiresAt })
        {
            domain.IssueCertificate(issuedAt, expiresAt);
            return Result<bool>.Success(true);
        }

        domain.MarkCertificateFailed();
        // IPersistsStateOnFailureCommand: precisa salvar explicitamente antes de devolver a falha.
        await _db.SaveChangesAsync(cancellationToken);

        await _platformAlertNotifier.NotifyDomainCertificateRenewalFailedAsync(
            request.TenantId, domain.Id, domain.Domain, domain.CertExpiresAt, cancellationToken);

        return Result<bool>.Failure(
            $"Falha ao renovar o certificado de {domain.Domain}.", ApiErrorCodes.TenantDomainCertificateIssuanceFailed);
    }
}
