using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Domain.Platform;
using MediatR;

namespace Nexora.Application.Tenants.Commands.RecordCrossTenantAccessAttempt;

internal sealed class RecordCrossTenantAccessAttemptCommandHandler
    : IRequestHandler<RecordCrossTenantAccessAttemptCommand, Result>
{
    private readonly IApplicationDbContext _db;

    public RecordCrossTenantAccessAttemptCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public Task<Result> Handle(RecordCrossTenantAccessAttemptCommand request, CancellationToken cancellationToken)
    {
        var entry = AuditLog.Create(
            tenantId: request.ActorTenantId,
            action: "tenant.cross_tenant_access_attempt",
            entity: "tenant",
            occurredAt: DateTimeOffset.UtcNow,
            actorId: request.ActorUserId,
            entityId: request.TargetTenantId,
            reason: "Usuário autenticado informou o ID de um estabelecimento que não é o seu em GET /v1/platform/tenants/{id}.",
            ip: request.Ip);

        _db.AuditLogs.Add(entry);

        // SaveChangesAsync é feito pelo TransactionBehavior (comando) — mesma transação do
        // registro de auditoria, sem passo extra aqui (ADR-006).
        return Task.FromResult(Result.Success());
    }
}
