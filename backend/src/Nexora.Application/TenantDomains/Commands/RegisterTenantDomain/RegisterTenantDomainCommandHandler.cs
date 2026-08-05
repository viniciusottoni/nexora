using System.Security.Cryptography;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.TenantDomains.Support;
using Nexora.Contracts.Platform;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.TenantDomains.Commands.RegisterTenantDomain;

/// <summary>
/// US-143 §3.1 "Cadastro de domínio ou subdomínio por tenant" — gera o token de verificação
/// (RN-015 exige unicidade global do domínio, checada aqui antes do índice único
/// <c>uq_tenant_domain_domain</c> devolver 500 em vez de um 422 amigável) e devolve as instruções
/// de DNS (§10, "linguagem que um cliente não técnico consiga repassar ao provedor").
/// </summary>
internal sealed class RegisterTenantDomainCommandHandler
    : IRequestHandler<RegisterTenantDomainCommand, Result<RegisterTenantDomainResponse>>
{
    private readonly IApplicationDbContext _db;

    public RegisterTenantDomainCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<RegisterTenantDomainResponse>> Handle(
        RegisterTenantDomainCommand request, CancellationToken cancellationToken)
    {
        var normalizedDomain = request.Domain.Trim().ToLowerInvariant();

        var tenantExists = await _db.Tenants.AsNoTracking()
            .AnyAsync(t => t.Id == request.TenantId && t.DeletedAt == null, cancellationToken);
        if (!tenantExists)
        {
            return Result<RegisterTenantDomainResponse>.Failure("Estabelecimento não encontrado.", ApiErrorCodes.TenantNotFound);
        }

        // RN-015 "cada domínio resolve exatamente um tenant" — checagem amigável antes do índice
        // único global; a varredura deixa app.tenant_id em algum tenant qualquer (ou no último
        // varrido), por isso SetTenantContextAsync abaixo fixa explicitamente o tenant certo antes
        // do INSERT, nunca reaproveitando o que sobrou da checagem.
        var alreadyRegistered = await TenantDomainPlatformLookup.ExistsAsync(_db, normalizedDomain, cancellationToken);
        if (alreadyRegistered)
        {
            return Result<RegisterTenantDomainResponse>.Failure(
                "Este domínio já está cadastrado.",
                ApiErrorCodes.TenantDomainAlreadyRegistered,
                new Dictionary<string, string[]> { ["domain"] = new[] { "Este domínio já está cadastrado." } });
        }

        await _db.SetTenantContextAsync(request.TenantId, cancellationToken);

        var verificationToken = CreateVerificationToken();
        var domain = TenantDomain.Register(request.TenantId, normalizedDomain, verificationToken);
        _db.TenantDomains.Add(domain);

        // SaveChangesAsync é feito pelo TransactionBehavior (ADR-006/ADR-037).

        var response = new RegisterTenantDomainResponse(
            domain.ToResponse(),
            new TenantDomainVerificationInstructionsResponse("TXT", domain.VerificationRecordName, domain.VerificationToken),
            domain.Status.ToWireLabel());

        return Result<RegisterTenantDomainResponse>.Success(response);
    }

    private static string CreateVerificationToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(24);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
