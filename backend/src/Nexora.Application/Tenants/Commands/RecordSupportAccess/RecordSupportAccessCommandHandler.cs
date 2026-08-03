using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tenants.Commands.RecordSupportAccess;

internal sealed class RecordSupportAccessCommandHandler : IRequestHandler<RecordSupportAccessCommand, Result>
{
    private readonly IApplicationDbContext _db;

    public RecordSupportAccessCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(RecordSupportAccessCommand request, CancellationToken cancellationToken)
    {
        var tenantExists = await _db.Tenants
            .AsNoTracking()
            .AnyAsync(t => t.Id == request.TenantId && t.DeletedAt == null, cancellationToken);

        if (!tenantExists)
        {
            return Result.Failure("Estabelecimento não encontrado.", ApiErrorCodes.TenantNotFound);
        }

        var now = DateTimeOffset.UtcNow;

        // O ator (staff de plataforma) não tem tenant próprio — sem este SET explícito,
        // current_tenant_id() ficaria nulo e a política tenant_isolation (WITH CHECK) recusaria o
        // INSERT no tenant alvo. Mesmo mecanismo de ProvisionTenantCommandHandler
        // (SetTenantContextAsync), documentado em AppDbContext.Auth.cs.
        await _db.SetTenantContextAsync(request.TenantId, cancellationToken);

        // EVT-074 support.access.granted — criado ANTES do AuditLog para correlacionar via
        // DomainEventId (E-09/US-090).
        var supportAccessEvent = DomainEvent.Create(
            request.TenantId,
            type: "support.access.granted",
            aggregateType: "tenant",
            aggregateId: request.TenantId,
            payload: JsonSerializer.Serialize(new { reason = request.Reason, durationMinutes = request.DurationMinutes }),
            origin: "CLOUD",
            occurredAt: now,
            actorId: request.SupportUserId);
        _db.DomainEvents.Add(supportAccessEvent);

        _db.AuditLogs.Add(AuditLog.Create(
            request.TenantId,
            action: "SUPPORT_ACCESS_GRANTED",
            entity: "tenant",
            occurredAt: now,
            actorId: request.SupportUserId,
            entityId: request.TenantId,
            reason: request.Reason,
            after: JsonSerializer.Serialize(new { durationMinutes = request.DurationMinutes }),
            domainEventId: supportAccessEvent.Id));

        // SaveChangesAsync é feito pelo TransactionBehavior.

        return Result.Success();
    }
}
