using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tenants.Commands.RevokeSupportAccess;

internal sealed class RevokeSupportAccessCommandHandler : IRequestHandler<RevokeSupportAccessCommand, Result>
{
    private readonly IApplicationDbContext _db;

    public RevokeSupportAccessCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(RevokeSupportAccessCommand request, CancellationToken cancellationToken)
    {
        // RLS (tenant_isolation) já restringe esta leitura ao tenant do chamador — o filtro
        // explícito por TenantId abaixo é defesa em profundidade (mesmo padrão de
        // GetAuditLogQueryHandler) e o que garante o 404 (nunca 403) de ADR-021 quando o id
        // pertence a outro tenant: para o chamador, essas linhas simplesmente não existem.
        var grant = await _db.SupportAccesses
            .SingleOrDefaultAsync(a => a.Id == request.SupportAccessId && a.TenantId == request.TenantId, cancellationToken);

        if (grant is null)
        {
            return Result.Failure("Acesso de suporte não encontrado.", ApiErrorCodes.SupportAccessNotFound);
        }

        var alreadyRevoked = grant.IsRevoked;
        var now = DateTimeOffset.UtcNow;
        grant.Revoke(request.RevokedBy, now);

        if (!alreadyRevoked)
        {
            // ADR-006: nenhuma transição de estado sem seu evento, na mesma transação do estado.
            var revokedEvent = DomainEvent.Create(
                request.TenantId,
                type: "support.access.revoked",
                aggregateType: "support_access",
                aggregateId: grant.Id,
                payload: JsonSerializer.Serialize(new { revokedBy = request.RevokedBy }),
                origin: "CLOUD",
                occurredAt: now,
                actorId: request.RevokedBy);
            _db.DomainEvents.Add(revokedEvent);

            _db.AuditLogs.Add(AuditLog.Create(
                request.TenantId,
                action: "SUPPORT_ACCESS_REVOKED",
                entity: "support_access",
                occurredAt: now,
                actorId: request.RevokedBy,
                entityId: grant.Id,
                domainEventId: revokedEvent.Id));
        }

        // SaveChangesAsync é feito pelo TransactionBehavior.

        return Result.Success();
    }
}
