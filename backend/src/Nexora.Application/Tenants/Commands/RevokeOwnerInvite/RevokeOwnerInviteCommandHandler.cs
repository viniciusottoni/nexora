using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tenants.Commands.RevokeOwnerInvite;

/// <summary>US-155 · Revogação explícita de um convite ainda pendente (RN-004: motivo sempre registrado).</summary>
internal sealed class RevokeOwnerInviteCommandHandler : IRequestHandler<RevokeOwnerInviteCommand, Result>
{
    private readonly IApplicationDbContext _db;

    public RevokeOwnerInviteCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(RevokeOwnerInviteCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return Result.Failure("O motivo é obrigatório.", ApiErrorCodes.ReasonRequired);
        }

        var tenant = await _db.Tenants
            .SingleOrDefaultAsync(t => t.Id == request.TenantId && t.DeletedAt == null, cancellationToken);

        if (tenant is null)
        {
            return Result.Failure("Estabelecimento não encontrado.", ApiErrorCodes.TenantNotFound);
        }

        await _db.SetTenantContextAsync(tenant.Id, cancellationToken);

        // RN-015: o filtro por TenantId (não só por Id) é o que garante que um convite de outro
        // tenant nunca é encontrado aqui — vira o mesmo 404 de "convite inexistente", nunca 403.
        var invite = await _db.OwnerInvites
            .SingleOrDefaultAsync(i => i.Id == request.InviteId && i.TenantId == tenant.Id, cancellationToken);

        if (invite is null)
        {
            return Result.Failure("Convite não encontrado.", ApiErrorCodes.OwnershipInviteNotFound);
        }

        if (invite.IsConsumed)
        {
            return Result.Failure("Este convite já foi aceito e não pode ser revogado.", ApiErrorCodes.OwnershipInviteAlreadyConsumed);
        }

        if (invite.IsRevoked)
        {
            return Result.Failure("Este convite já foi revogado.", ApiErrorCodes.OwnershipInviteAlreadyRevoked);
        }

        invite.Revoke(request.Reason);

        var now = DateTimeOffset.UtcNow;
        var domainEvent = DomainEvent.Create(
            tenant.Id,
            type: "tenant.owner_access_changed",
            aggregateType: "owner_invite",
            aggregateId: invite.Id,
            payload: JsonSerializer.Serialize(new
            {
                tenantId = tenant.Id,
                action = "INVITE_REVOKED",
                userId = invite.UserId,
                inviteId = invite.Id,
                previousOwnerId = (Guid?)null,
                actorId = request.ActorId,
            }),
            origin: "CLOUD",
            occurredAt: now,
            actorId: request.ActorId);
        _db.DomainEvents.Add(domainEvent);

        _db.AuditLogs.Add(AuditLog.Create(
            tenant.Id,
            action: "OWNER_INVITE_REVOKED",
            entity: "owner_invite",
            occurredAt: now,
            actorId: request.ActorId,
            entityId: invite.Id,
            reason: request.Reason,
            domainEventId: domainEvent.Id));

        return Result.Success();
    }
}
