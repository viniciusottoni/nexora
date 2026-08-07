using System.Security.Cryptography;
using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Installations.Abstractions;
using Nexora.Application.Installations.Support;
using Nexora.Contracts.Platform;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Nexora.Application.Installations.Commands.ReissueInstallationToken;

/// <summary>
/// US-156 · Recuperação do provisionamento e token de instalação — cenários Gherkin "Resposta de
/// criação foi perdida" e "Reemissão segura". Revoga ATOMICAMENTE (mesma transação, ver
/// <c>TransactionBehavior</c>) toda credencial pendente anterior da instalação e emite uma nova,
/// mantendo o MESMO tenant/loja/instalação (nunca duplica).
/// </summary>
/// <remarks>
/// <para>
/// <b>Convivência com os campos legados:</b> além de criar a linha de auditoria/histórico em
/// <see cref="InstallationCredential"/>, este handler chama
/// <see cref="EdgeInstallation.IssueInstallToken"/> — o MESMO método já usado por
/// <c>ProvisionTenantCommandHandler</c> na emissão original — para que
/// <see cref="EdgeInstallation.InstallTokenHash"/>/<see cref="EdgeInstallation.TokenExpiresAt"/>
/// sempre reflitam o token MAIS RECENTE. É isso que faz "o anterior deixar de funcionar
/// imediatamente" (Gherkin "Reemissão segura"): o hash antigo simplesmente para de bater com a
/// única coluna que <c>ConsumeInstallationTokenCommandHandler</c> compara.
/// </para>
/// <para>
/// <b>Digest canônico:</b> provisionamento, reemissão, consumo e registro usam
/// <see cref="IInstallationTokenDigester"/>. Convites e outros segredos continuam usando
/// <c>ISecretDigester</c>. Manter algoritmos separados impede que um token emitido seja persistido
/// com um digest diferente daquele calculado pelo protocolo de instalação.
/// </para>
/// </remarks>
internal sealed class ReissueInstallationTokenCommandHandler
    : IRequestHandler<ReissueInstallationTokenCommand, Result<ReissueInstallationTokenResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IInstallationTokenDigester _tokenDigester;
    private readonly ILogger<ReissueInstallationTokenCommandHandler> _logger;

    public ReissueInstallationTokenCommandHandler(
        IApplicationDbContext db, IInstallationTokenDigester tokenDigester, ILogger<ReissueInstallationTokenCommandHandler> logger)
    {
        _db = db;
        _tokenDigester = tokenDigester;
        _logger = logger;
    }

    public async Task<Result<ReissueInstallationTokenResponse>> Handle(
        ReissueInstallationTokenCommand request, CancellationToken cancellationToken)
    {
        var installation = await PlatformInstallationLookup.FindAsync(_db, request.InstallationId, cancellationToken);
        if (installation is null)
        {
            return Result<ReissueInstallationTokenResponse>.Failure(
                "Instalação não encontrada.", ApiErrorCodes.InstallationNotFound);
        }

        // PlatformInstallationLookup já deixa app.tenant_id fixado no tenant dono (última iteração
        // que encontrou a instalação) — reafirma explicitamente para não depender dessa ordem
        // interna (mesma convenção de GetInstallationDiagnosticsQueryHandler).
        await _db.SetTenantContextAsync(installation.TenantId, cancellationToken);

        // Concorrência (US-156, cenário "duas reemissões simultâneas") — SELECT ... FOR UPDATE
        // serializa quem chegar aqui para a MESMA instalação: a segunda transação só lê depois que
        // a primeira tiver COMMITADO (TransactionBehavior envolve todo o comando em uma transação
        // real), então enxerga o estado já rotacionado e revoga a credencial recém-emitida pela
        // primeira antes de emitir a sua própria — só sobra uma credencial ativa ao final das duas.
        var locked = await _db.LockEdgeInstallationForUpdateAsync(installation.Id, cancellationToken);
        if (locked is null)
        {
            return Result<ReissueInstallationTokenResponse>.Failure(
                "Instalação não encontrada.", ApiErrorCodes.InstallationNotFound);
        }

        // Gherkin "Token já consumido" — a instalação já concluiu o pareamento; a via correta deixa
        // de ser "recuperar o provisionamento" e passa a ser o fluxo de manutenção do parque
        // instalado (US-146).
        if (!locked.CanReissueToken)
        {
            return Result<ReissueInstallationTokenResponse>.Failure(
                "Esta instalação já concluiu o pareamento. Utilize o fluxo de manutenção do parque instalado.",
                ApiErrorCodes.InstallationAlreadyRegistered);
        }

        var now = DateTimeOffset.UtcNow;

        // Revogação atômica de toda credencial ainda pendente da MESMA instalação (US-156 "rotação/
        // revogação atômica de todos os tokens pendentes anteriores") — na prática, no máximo uma
        // (o próprio fluxo normal já revoga a anterior a cada reemissão), mas o laço cobre qualquer
        // inconsistência histórica sem deixar nada pendente para trás.
        var pendingCredentials = await _db.InstallationCredentials
            .Where(c => c.InstallationId == locked.Id && c.RevokedAt == null && c.ConsumedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var pending in pendingCredentials)
        {
            pending.Revoke(now);
        }

        var rawToken = CreateRawSecret();
        var tokenHash = _tokenDigester.Digest(rawToken);
        var expiresAt = now.AddHours(request.ExpiresInHours);

        var credential = InstallationCredential.Issue(
            locked.TenantId, locked.Id, tokenHash, expiresAt, request.Reason, request.ActorId);
        _db.InstallationCredentials.Add(credential);

        // Mantém os campos legados sincronizados com o token MAIS RECENTE — ver docstring da classe.
        locked.IssueInstallToken(tokenHash, expiresAt);

        _db.AuditLogs.Add(AuditLog.Create(
            locked.TenantId,
            action: "INSTALLATION_TOKEN_REISSUED",
            entity: nameof(EdgeInstallation),
            occurredAt: now,
            storeId: locked.StoreId,
            actorId: request.ActorId,
            entityId: locked.Id,
            // RN-004: NUNCA o segredo bruto nem o hash — só o id da credencial e a validade.
            after: JsonSerializer.Serialize(new { credentialId = credential.Id, expiresAt }),
            reason: request.Reason));

        _db.DomainEvents.Add(DomainEvent.Create(
            locked.TenantId,
            type: "installation.token_reissued", // EVT-059
            aggregateType: nameof(EdgeInstallation),
            aggregateId: locked.Id,
            // Nunca token bruto nem hash (RN-004) — previousCredentialId é nulo na primeiríssima
            // reemissão após o provisionamento original (o token inicial nunca ganhou uma linha de
            // InstallationCredential — ver docstring da classe/relatório da tarefa).
            payload: JsonSerializer.Serialize(new
            {
                tenantId = locked.TenantId,
                installationId = locked.Id,
                previousCredentialId = pendingCredentials.Count > 0 ? pendingCredentials[0].Id : (Guid?)null,
                credentialId = credential.Id,
                expiresAt,
                actorId = request.ActorId,
            }),
            origin: "CLOUD",
            occurredAt: now,
            storeId: locked.StoreId,
            actorId: request.ActorId,
            installationId: locked.Id));

        _logger.LogInformation(
            "Token de instalação reemitido. InstallationId={InstallationId}, CredentialId={CredentialId}",
            locked.Id, credential.Id);

        var response = new ReissueInstallationTokenResponse(
            credential.Id,
            expiresAt,
            rawToken,
            $"./install.sh --tenant={locked.TenantId} --token={rawToken}");

        return Result<ReissueInstallationTokenResponse>.Success(response);
    }

    /// <summary>Mesma técnica de <c>ProvisionTenantCommandHandler.CreateRawSecret</c> (32 bytes aleatórios, base64url sem padding) — não inventa outra.</summary>
    private static string CreateRawSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
