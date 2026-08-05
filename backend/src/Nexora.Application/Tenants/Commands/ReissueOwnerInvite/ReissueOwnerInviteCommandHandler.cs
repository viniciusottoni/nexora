using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Notifications;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Tenants.Support;
using Nexora.Contracts.Tenants;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tenants.Commands.ReissueOwnerInvite;

/// <summary>
/// US-155 · Proprietários, usuários iniciais e convites — reenvio/correção do convite de dono
/// (ver docstring de <see cref="ReissueOwnerInviteCommand"/> sobre por que os dois cenários Gherkin
/// compartilham este único handler). Só permitido enquanto o proprietário ainda está
/// <see cref="UserStatus.Invited"/> — depois de aceito (<see cref="UserStatus.Active"/>), "corrigir o
/// convite" deixaria de fazer sentido (a credencial já existe).
/// </summary>
internal sealed class ReissueOwnerInviteCommandHandler
    : IRequestHandler<ReissueOwnerInviteCommand, Result<CreateOwnerInviteResponse>>
{
    private const string OwnerInviteEmailTemplate = "owner-invite";
    private static readonly TimeSpan OwnerInviteTtl = TimeSpan.FromHours(72);

    private readonly IApplicationDbContext _db;
    private readonly ISecretDigester _secretDigester;
    private readonly IEmailSender _emailSender;

    public ReissueOwnerInviteCommandHandler(IApplicationDbContext db, ISecretDigester secretDigester, IEmailSender emailSender)
    {
        _db = db;
        _secretDigester = secretDigester;
        _emailSender = emailSender;
    }

    public async Task<Result<CreateOwnerInviteResponse>> Handle(ReissueOwnerInviteCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return Result<CreateOwnerInviteResponse>.Failure("O motivo é obrigatório.", ApiErrorCodes.ReasonRequired);
        }

        var tenant = await _db.Tenants
            .SingleOrDefaultAsync(t => t.Id == request.TenantId && t.DeletedAt == null, cancellationToken);

        if (tenant is null)
        {
            return Result<CreateOwnerInviteResponse>.Failure("Estabelecimento não encontrado.", ApiErrorCodes.TenantNotFound);
        }

        await _db.SetTenantContextAsync(tenant.Id, cancellationToken);

        var owner = await (
            from userRole in _db.UserRoles
            join role in _db.Roles on userRole.RoleId equals role.Id
            join user in _db.Users on userRole.UserId equals user.Id
            where userRole.TenantId == tenant.Id && role.Code == "OWNER"
            select user
        ).FirstOrDefaultAsync(cancellationToken);

        if (owner is null)
        {
            return Result<CreateOwnerInviteResponse>.Failure("Proprietário não encontrado.", ApiErrorCodes.OwnershipOwnerNotFound);
        }

        if (owner.Status != UserStatus.Invited)
        {
            return Result<CreateOwnerInviteResponse>.Failure(
                "O proprietário já aceitou o convite — não é possível reenviar ou corrigir.",
                ApiErrorCodes.OwnershipOwnerAlreadyActive);
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var emailChanged = !string.Equals(owner.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase);

        if (emailChanged)
        {
            var existingCredential = await _db.FindLoginCredentialByEmailAsync(normalizedEmail, cancellationToken);
            if (existingCredential is not null && existingCredential.UserId != owner.Id)
            {
                return Result<CreateOwnerInviteResponse>.Failure(
                    "Este e-mail já pertence a outro usuário.", ApiErrorCodes.OwnershipEmailAlreadyInUse);
            }
        }

        // "Qualquer token anterior deve ser invalidado" (Gherkin "Convite expirado") — revoga TODO
        // convite ainda não consumido/revogado deste proprietário, não só o mais recente (defensivo:
        // não deveria existir mais de um pendente, mas nada impede tecnicamente).
        var previousInvites = await _db.OwnerInvites
            .Where(i => i.TenantId == tenant.Id && i.UserId == owner.Id && i.ConsumedAt == null && i.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var previousInvite in previousInvites)
        {
            previousInvite.Revoke(request.Reason);
        }

        owner.CorrectInviteDetails(request.Name, normalizedEmail);

        if (emailChanged)
        {
            tenant.SetOwnerEmail(normalizedEmail);
        }

        var now = DateTimeOffset.UtcNow;
        var rawSecret = OwnerInviteSecretFactory.CreateRawSecret();
        var secretHash = _secretDigester.Digest(rawSecret);

        await _emailSender.EnqueueAsync(
            tenant.Id,
            normalizedEmail,
            OwnerInviteEmailTemplate,
            new Dictionary<string, string>
            {
                ["token"] = rawSecret,
                ["tenantName"] = tenant.Name,
                ["ownerName"] = owner.Name
            },
            cancellationToken);

        // IEmailSender.EnqueueAsync não devolve a linha criada (só enfileira, ver docstring de
        // EmailOutboxSender) — não editamos a interface para não colidir com outros agentes
        // (docstring da classe, "não editar em paralelo"). EmailOutbox.Create já gera o Id
        // (IdGenerator.NewId()) ANTES do SaveChangesAsync, então a entidade recém-adicionada já
        // aparece com Id definido no change tracker local (.Local) — nenhuma query ao banco.
        var outboxEntry = _db.EmailOutboxes.Local
            .Where(e => e.TenantId == tenant.Id && e.Recipient == normalizedEmail && e.Template == OwnerInviteEmailTemplate)
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefault();

        var invite = OwnerInvite.Create(
            tenant.Id,
            owner.Id,
            normalizedEmail,
            secretHash,
            now.Add(OwnerInviteTtl),
            reason: request.Reason,
            emailOutboxId: outboxEntry?.Id);
        _db.OwnerInvites.Add(invite);

        var action = emailChanged ? "EMAIL_CORRECTED" : "REINVITED";

        var domainEvent = DomainEvent.Create(
            tenant.Id,
            type: "tenant.owner_access_changed",
            aggregateType: "owner_invite",
            aggregateId: invite.Id,
            payload: JsonSerializer.Serialize(new
            {
                tenantId = tenant.Id,
                action,
                userId = owner.Id,
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
            action: $"OWNER_INVITE_{action}",
            entity: "owner_invite",
            occurredAt: now,
            actorId: request.ActorId,
            entityId: invite.Id,
            reason: request.Reason,
            domainEventId: domainEvent.Id));

        return Result<CreateOwnerInviteResponse>.Success(
            new CreateOwnerInviteResponse(invite.Id, normalizedEmail, invite.ExpiresAt));
    }
}
