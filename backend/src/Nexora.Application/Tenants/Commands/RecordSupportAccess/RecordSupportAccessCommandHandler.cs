using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Notifications;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Contracts.Platform;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tenants.Commands.RecordSupportAccess;

internal sealed class RecordSupportAccessCommandHandler
    : IRequestHandler<RecordSupportAccessCommand, Result<GrantSupportAccessResponse>>
{
    private const string SupportAccessGrantedTemplate = "support-access-granted";
    private const string OwnerRoleCode = "OWNER";

    private readonly IApplicationDbContext _db;
    private readonly ISecretDigester _secretDigester;
    private readonly IEmailSender _emailSender;

    public RecordSupportAccessCommandHandler(
        IApplicationDbContext db, ISecretDigester secretDigester, IEmailSender emailSender)
    {
        _db = db;
        _secretDigester = secretDigester;
        _emailSender = emailSender;
    }

    public async Task<Result<GrantSupportAccessResponse>> Handle(
        RecordSupportAccessCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants
            .AsNoTracking()
            .Where(t => t.Id == request.TenantId && t.DeletedAt == null)
            .Select(t => new { t.Id, t.Name })
            .SingleOrDefaultAsync(cancellationToken);

        if (tenant is null)
        {
            return Result<GrantSupportAccessResponse>.Failure("Estabelecimento não encontrado.", ApiErrorCodes.TenantNotFound);
        }

        var now = DateTimeOffset.UtcNow;

        // O ator (staff de plataforma) não tem tenant próprio — sem este SET explícito,
        // current_tenant_id() ficaria nulo e a política tenant_isolation (WITH CHECK) recusaria o
        // INSERT no tenant alvo. Mesmo mecanismo de ProvisionTenantCommandHandler
        // (SetTenantContextAsync), documentado em AppDbContext.Auth.cs.
        await _db.SetTenantContextAsync(request.TenantId, cancellationToken);

        // US-145 §7: token de escopo especial — o valor bruto só existe nesta resposta, nunca
        // persistido (mesma convenção de ProvisionTenantCommandHandler.CreateRawSecret/installToken).
        var rawToken = CreateRawSecret();
        var tokenHash = _secretDigester.Digest(rawToken);

        var grant = SupportAccess.Grant(
            request.TenantId, request.SupportUserId, request.Reason, request.DurationMinutes, tokenHash, now);
        _db.SupportAccesses.Add(grant);

        // EVT-074 support.access.granted — criado ANTES do AuditLog para correlacionar via
        // DomainEventId (E-09/US-090).
        var supportAccessEvent = DomainEvent.Create(
            request.TenantId,
            type: "support.access.granted",
            aggregateType: "support_access",
            aggregateId: grant.Id,
            payload: JsonSerializer.Serialize(new { reason = request.Reason, durationMinutes = request.DurationMinutes }),
            origin: "CLOUD",
            occurredAt: now,
            actorId: request.SupportUserId);
        _db.DomainEvents.Add(supportAccessEvent);

        _db.AuditLogs.Add(AuditLog.Create(
            request.TenantId,
            action: "SUPPORT_ACCESS_GRANTED",
            entity: "support_access",
            occurredAt: now,
            actorId: request.SupportUserId,
            entityId: grant.Id,
            reason: request.Reason,
            after: JsonSerializer.Serialize(new { durationMinutes = request.DurationMinutes, expiresAt = grant.ExpiresAt }),
            domainEventId: supportAccessEvent.Id));

        // US-145 §10 "Notificação ao cliente no momento da concessão, não depois" — enfileirado na
        // MESMA transação do estado (ADR-006), entregue de fato pelo EmailOutboxDeliveryWorker
        // (mesmo mecanismo de convite de dono). Notifica todo AppUser com papel OWNER do tenant —
        // não há um único "contato" de tenant modelado hoje (ver relatório da tarefa).
        var ownerEmails = await _db.UserRoles
            .Where(ur => ur.TenantId == request.TenantId)
            .Join(_db.Roles.Where(r => r.TenantId == request.TenantId && r.Code == OwnerRoleCode),
                ur => ur.RoleId, r => r.Id, (ur, r) => ur.UserId)
            .Join(_db.Users.Where(u => u.TenantId == request.TenantId && u.DeletedAt == null && u.Email != null),
                userId => userId, u => u.Id, (userId, u) => u.Email!)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var ownerEmail in ownerEmails)
        {
            await _emailSender.EnqueueAsync(
                request.TenantId,
                ownerEmail,
                SupportAccessGrantedTemplate,
                new Dictionary<string, string>
                {
                    ["tenantName"] = tenant.Name,
                    ["reason"] = request.Reason,
                    ["durationMinutes"] = request.DurationMinutes.ToString(CultureInfo.InvariantCulture),
                    ["expiresAt"] = grant.ExpiresAt.ToString("O"),
                },
                cancellationToken);
        }

        // SaveChangesAsync é feito pelo TransactionBehavior.

        var response = new GrantSupportAccessResponse(rawToken, grant.ExpiresAt, ownerEmails.Count > 0);
        return Result<GrantSupportAccessResponse>.Success(response);
    }

    private static string CreateRawSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
