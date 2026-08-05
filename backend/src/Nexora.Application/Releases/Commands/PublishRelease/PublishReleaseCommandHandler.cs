using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Contracts.Platform;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Releases.Commands.PublishRelease;

/// <summary>
/// US-146 §3.1/§4 "Liberação gradual" — <c>version</c> é a chave natural (índice único
/// <c>uq_release_version</c>): a PRIMEIRA publicação de uma versão cria a <see cref="Release"/>;
/// uma publicação SUBSEQUENTE da MESMA versão amplia o rollout já em curso
/// (<see cref="Release.ExpandRollout"/>) em vez de falhar — é exatamente como o cenário Gherkin
/// "Liberação gradual" descreve o operador de plataforma avançando o rollout ao longo do tempo
/// (10% hoje, 50% amanhã, 100% depois de confirmar que não houve falha).
/// </summary>
/// <remarks>
/// Decisão registrada (pedida explicitamente pela tarefa): re-publicar a MESMA versão com um
/// <c>rolloutPercent</c> MENOR que o já liberado não é um no-op silencioso — é rejeitado com
/// <see cref="ApiErrorCodes.ReleaseRolloutCannotDecrease"/>. <see cref="Release.ExpandRollout"/>
/// já impõe a regra de domínio "nunca reduz"; se o handler deixasse passar sem avisar, um operador
/// de plataforma que discou o percentual errado acharia que a liberação regrediu quando na
/// verdade nada mudou — um erro claro é mais seguro que uma falsa sensação de sucesso.
/// </remarks>
internal sealed class PublishReleaseCommandHandler : IRequestHandler<PublishReleaseCommand, Result<PublishReleaseResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public PublishReleaseCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PublishReleaseResponse>> Handle(PublishReleaseCommand request, CancellationToken cancellationToken)
    {
        var existing = await _db.Releases.FirstOrDefaultAsync(r => r.Version == request.Version, cancellationToken);

        if (existing is not null)
        {
            if (request.RolloutPercent < existing.RolloutPercent)
            {
                return Result<PublishReleaseResponse>.Failure(
                    $"A versão {request.Version} já está liberada para {existing.RolloutPercent}% do parque — a liberação gradual nunca reduz.",
                    ApiErrorCodes.ReleaseRolloutCannotDecrease);
            }

            existing.ExpandRollout(request.RolloutPercent);

            // SaveChangesAsync é feito pelo TransactionBehavior (command).
            return Result<PublishReleaseResponse>.Success(new PublishReleaseResponse(ToResponse(existing)));
        }

        var release = Release.Publish(request.Version, request.RolloutPercent, request.Notes, _tenantContext.UserId);
        _db.Releases.Add(release);

        // SaveChangesAsync é feito pelo TransactionBehavior (command).
        return Result<PublishReleaseResponse>.Success(new PublishReleaseResponse(ToResponse(release)));
    }

    private static ReleaseResponse ToResponse(Release release) => new(
        release.Id,
        release.Version,
        release.RolloutPercent,
        release.Notes,
        release.PublishedAt,
        release.PublishedBy);
}
