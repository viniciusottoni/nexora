using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Platform.SupportAccessTokens;

/// <summary>
/// Implementação de <see cref="ISupportAccessTokenValidator"/> — fora do pipeline MediatR de
/// propósito (é um serviço de infraestrutura de autorização consumido POR outros handlers/
/// middlewares, não uma ação de negócio disparada pelo usuário) e por isso chama
/// <see cref="IApplicationDbContext.SaveChangesAsync"/> diretamente ao registrar o uso do token
/// (<see cref="Domain.Platform.SupportAccess.RecordUsage"/>), como <c>EmailOutboxDeliveryWorker</c>
/// já faz fora do <c>TransactionBehavior</c>.
/// </summary>
public sealed class SupportAccessTokenValidator : ISupportAccessTokenValidator
{
    private readonly IApplicationDbContext _db;
    private readonly ISecretDigester _secretDigester;

    public SupportAccessTokenValidator(IApplicationDbContext db, ISecretDigester secretDigester)
    {
        _db = db;
        _secretDigester = secretDigester;
    }

    public async Task<Result<SupportAccessTokenValidationResult>> ValidateAsync(
        Guid tenantId, string rawToken, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            var (message, code) = SupportAccessTokenPolicy.FailureFor(SupportAccessTokenStatus.NotFound);
            return Result<SupportAccessTokenValidationResult>.Failure(message, code);
        }

        var tokenHash = _secretDigester.Digest(rawToken);

        // Sem este SET explícito, current_tenant_id() ficaria nulo (nenhuma sessão autenticada do
        // PRÓPRIO tenant está em curso aqui — quem chama é o fluxo de suporte da plataforma) e o
        // RLS (USING) negaria a leitura por padrão. Mesmo mecanismo de
        // RecordSupportAccessCommandHandler.
        await _db.SetTenantContextAsync(tenantId, cancellationToken);

        var access = await _db.SupportAccesses
            .SingleOrDefaultAsync(a => a.TenantId == tenantId && a.TokenHash == tokenHash, cancellationToken);

        var status = SupportAccessTokenPolicy.Evaluate(access, now);
        if (status != SupportAccessTokenStatus.Valid)
        {
            var (message, code) = SupportAccessTokenPolicy.FailureFor(status);
            return Result<SupportAccessTokenValidationResult>.Failure(message, code);
        }

        access!.RecordUsage(now);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<SupportAccessTokenValidationResult>.Success(
            new SupportAccessTokenValidationResult(access.Id, access.TenantId, access.GrantedTo));
    }
}
