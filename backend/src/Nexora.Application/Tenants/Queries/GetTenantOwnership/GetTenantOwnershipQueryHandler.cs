using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Contracts.Tenants;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tenants.Queries.GetTenantOwnership;

/// <summary>
/// US-155 · Proprietários, usuários iniciais e convites — leitura administrativa completa do acesso
/// inicial do proprietário: quem é (ou <c>NONE</c> quando não há), estado do acesso, histórico de
/// convites (com entrega/aceitação/revogação, NUNCA segredo) e histórico de transferências.
/// Resiliência de seção (mesmo espírito de <c>GetTenantOverviewQueryHandler</c>, US-152): ausência de
/// proprietário resolvido NUNCA derruba a resposta — vira <c>owner.status == "NONE"</c>.
/// </summary>
internal sealed class GetTenantOwnershipQueryHandler : IRequestHandler<GetTenantOwnershipQuery, Result<TenantOwnershipResponse>>
{
    private readonly IApplicationDbContext _db;

    public GetTenantOwnershipQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<TenantOwnershipResponse>> Handle(GetTenantOwnershipQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants
            .AsNoTracking()
            .Where(t => t.Id == request.TenantId && t.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);

        if (tenant is null)
        {
            return Result<TenantOwnershipResponse>.Failure("Estabelecimento não encontrado.", ApiErrorCodes.TenantNotFound);
        }

        // RLS (ADR-004): user_role/role/app_user/owner_invite/email_outbox/ownership_transfer exigem
        // app.tenant_id fixado antes de qualquer leitura — mesmo mecanismo de GetTenantOverviewQueryHandler.
        await _db.SetTenantContextAsync(tenant.Id, cancellationToken);

        var ownerUser = await (
            from userRole in _db.UserRoles.AsNoTracking()
            join role in _db.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            join user in _db.Users.AsNoTracking() on userRole.UserId equals user.Id
            where userRole.TenantId == tenant.Id && role.Code == "OWNER"
            select user
        ).FirstOrDefaultAsync(cancellationToken);

        var ownerResponse = ownerUser is null
            ? new TenantOwnershipOwnerResponse(null, null, null, "NONE")
            : new TenantOwnershipOwnerResponse(ownerUser.Id, ownerUser.Name, ownerUser.Email ?? tenant.OwnerEmail, ToWireStatus(ownerUser.Status));

        var invites = ownerUser is null
            ? new List<TenantOwnershipInviteResponse>()
            : await BuildInviteHistoryAsync(tenant.Id, ownerUser.Id, cancellationToken);

        var transfers = await _db.OwnershipTransfers.AsNoTracking()
            .Where(t => t.TenantId == tenant.Id)
            .OrderByDescending(t => t.TransferredAt)
            .Select(t => new TenantOwnershipTransferHistoryResponse(
                t.Id, t.PreviousOwnerUserId, t.NewOwnerUserId, t.Reason, t.PreviousKeptAsAdmin, t.TransferredAt))
            .ToListAsync(cancellationToken);

        return Result<TenantOwnershipResponse>.Success(new TenantOwnershipResponse(ownerResponse, invites, transfers));
    }

    private async Task<List<TenantOwnershipInviteResponse>> BuildInviteHistoryAsync(Guid tenantId, Guid ownerUserId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var ownerInvites = await _db.OwnerInvites.AsNoTracking()
            .Where(i => i.TenantId == tenantId && i.UserId == ownerUserId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);

        var outboxIds = ownerInvites.Where(i => i.EmailOutboxId is not null).Select(i => i.EmailOutboxId!.Value).ToList();
        var deliveryStatusByOutboxId = outboxIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.EmailOutboxes.AsNoTracking()
                .Where(e => outboxIds.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id, e => e.Status, cancellationToken);

        return ownerInvites
            .Select(invite => new TenantOwnershipInviteResponse(
                invite.Id,
                invite.Email,
                invite.ResolveStatus(now),
                invite.EmailOutboxId is { } outboxId && deliveryStatusByOutboxId.TryGetValue(outboxId, out var status) ? status : "UNKNOWN",
                invite.CreatedAt,
                invite.ExpiresAt,
                invite.ConsumedAt,
                invite.RevokedAt,
                invite.RevokedReason,
                invite.Reason))
            .ToList();
    }

    /// <summary>US-155 §"Estado do acesso": convidado, ativo, bloqueado — <see cref="UserStatus.Inactive"/> não faz parte dos três estados do texto da US, incluído aqui só por exaustividade do switch.</summary>
    private static string ToWireStatus(UserStatus status) => status switch
    {
        UserStatus.Invited => "INVITED",
        UserStatus.Active => "ACTIVE",
        UserStatus.Blocked => "BLOCKED",
        UserStatus.Inactive => "INACTIVE",
        _ => "NONE"
    };
}
