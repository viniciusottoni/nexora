using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Platform;
using Nexora.Application.TenantDomains.Support;
using Nexora.Contracts.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.TenantDomains.Commands.VerifyTenantDomain;

/// <summary>
/// US-143 §3.1 "Verificação de propriedade por registro DNS" + "Emissão e renovação automática de
/// certificado TLS". Cenário "Domínio não verificado" (§4): a falha devolve uma mensagem que
/// repete exatamente o registro TXT esperado — a US pede "instruções claras do que fazer", não só
/// um código de erro. Emissão de certificado (cenário "Certificado automático") roda logo em
/// seguida à verificação bem-sucedida; falha na emissão NÃO desfaz a verificação (o domínio já é
/// legitimamente do tenant) — só marca <c>CertStatus.Failed</c>, que o
/// <c>TenantDomainCertificateRenewalWorker</c> tentará de novo depois (mesmo tratamento de
/// <c>IsCertificateExpiringSoon</c>, já que um certificado nunca emitido também não teria
/// <c>CertExpiresAt</c> — a primeira tentativa falha fica visível só pelo <c>CertStatus</c>, sem
/// alerta automático de renovação até haver ao menos uma emissão bem-sucedida; aceitável para esta
/// história, o próximo passo natural seria o worker também variar por "nunca emitido").
/// </summary>
internal sealed class VerifyTenantDomainCommandHandler
    : IRequestHandler<VerifyTenantDomainCommand, Result<VerifyTenantDomainResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IDomainVerificationService _domainVerificationService;
    private readonly ICertificateIssuer _certificateIssuer;

    public VerifyTenantDomainCommandHandler(
        IApplicationDbContext db,
        IDomainVerificationService domainVerificationService,
        ICertificateIssuer certificateIssuer)
    {
        _db = db;
        _domainVerificationService = domainVerificationService;
        _certificateIssuer = certificateIssuer;
    }

    public async Task<Result<VerifyTenantDomainResponse>> Handle(
        VerifyTenantDomainCommand request, CancellationToken cancellationToken)
    {
        var domain = await TenantDomainPlatformLookup.FindByIdAsync(_db, request.DomainId, cancellationToken);
        if (domain is null)
        {
            return Result<VerifyTenantDomainResponse>.Failure("Domínio não encontrado.", ApiErrorCodes.TenantDomainNotFound);
        }

        var hasTxtRecord = await _domainVerificationService.HasTxtRecordAsync(
            domain.VerificationRecordName, domain.VerificationToken, cancellationToken);

        if (!hasTxtRecord)
        {
            domain.MarkVerificationFailed();
            // IPersistsStateOnFailureCommand: TransactionBehavior não salva sozinho quando o
            // resultado é falha — o handler precisa persistir explicitamente (mesmo idioma de
            // PairDeviceCommandHandler.SaveAttemptAsync).
            await _db.SaveChangesAsync(cancellationToken);

            return Result<VerifyTenantDomainResponse>.Failure(
                $"Registro TXT não encontrado. Crie um registro do tipo TXT chamado " +
                $"\"{domain.VerificationRecordName}\" com o valor \"{domain.VerificationToken}\" no " +
                $"provedor de DNS de {domain.Domain} e tente verificar novamente.",
                ApiErrorCodes.TenantDomainVerificationFailed);
        }

        var now = DateTimeOffset.UtcNow;
        domain.MarkVerified(now);

        // Primeiro domínio verificado do tenant vira o primário — é o que a resolução de tenant
        // por host em tempo de requisição de fato usa (ver docstring de Tenant.SetCustomDomain).
        var hasPrimaryAlready = await _db.TenantDomains.AsNoTracking()
            .AnyAsync(d => d.TenantId == domain.TenantId && d.IsPrimary && d.DeletedAt == null, cancellationToken);

        if (!hasPrimaryAlready)
        {
            domain.MarkAsPrimary();

            var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == domain.TenantId, cancellationToken);
            tenant?.SetCustomDomain(domain.Domain);
        }

        var issuance = await _certificateIssuer.IssueAsync(domain.Domain, cancellationToken);
        if (issuance is { Succeeded: true, IssuedAt: { } issuedAt, ExpiresAt: { } expiresAt })
        {
            domain.IssueCertificate(issuedAt, expiresAt);
        }
        else
        {
            domain.MarkCertificateFailed();
        }

        return Result<VerifyTenantDomainResponse>.Success(new VerifyTenantDomainResponse(domain.ToResponse()));
    }
}
