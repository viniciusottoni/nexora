using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Tenants.Support;
using Nexora.Contracts.Tenants;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tenants.Commands.TransitionTenantStatus;

/// <summary>
/// US-153 · Ciclo de vida do estabelecimento — única porta de escrita para
/// suspender/reativar/cancelar um estabelecimento. Formaliza a máquina de estados canônica
/// (<see cref="TenantStatusTransitions"/>), grava histórico imutável (<see cref="TenantStatusHistory"/>)
/// e <see cref="AuditLog"/>, e emite <c>tenant.status_changed</c> (EVT-056) na MESMA transação
/// (ADR-006) — <c>SaveChangesAsync</c> é feito pelo <c>TransactionBehavior</c>.
/// </summary>
internal sealed class TransitionTenantStatusCommandHandler
    : IRequestHandler<TransitionTenantStatusCommand, Result<TenantStatusTransitionResponse>>
{
    private const string AdminOrigin = "PLATFORM_ADMIN";

    private readonly IApplicationDbContext _db;

    public TransitionTenantStatusCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<TenantStatusTransitionResponse>> Handle(
        TransitionTenantStatusCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants
            .SingleOrDefaultAsync(t => t.Id == request.TenantId && t.DeletedAt == null, cancellationToken);

        if (tenant is null)
        {
            return Result<TenantStatusTransitionResponse>.Failure(
                "Estabelecimento não encontrado.", ApiErrorCodes.TenantNotFound);
        }

        // Concorrência (doc §4, cenário "Concorrência") — checado ANTES de qualquer outra regra:
        // se o admin está olhando uma versão desatualizada, nem o motivo nem o alvo importam ainda.
        if (tenant.StatusVersion != request.ExpectedVersion)
        {
            return Result<TenantStatusTransitionResponse>.Failure(
                "Este estabelecimento foi alterado por outra sessão. Recarregue e tente novamente.",
                ApiErrorCodes.ConcurrencyConflict);
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return Result<TenantStatusTransitionResponse>.Failure(
                "O motivo da transição é obrigatório.", ApiErrorCodes.ReasonRequired);
        }

        if (!Enum.TryParse<TenantStatus>(request.TargetStatus, ignoreCase: true, out var target) ||
            !TenantStatusTransitions.AdminTargetsFrom(tenant.Status).Contains(target))
        {
            return Result<TenantStatusTransitionResponse>.Failure(
                $"Não é possível mudar de {tenant.Status.ToWireLabel()} para {request.TargetStatus.ToUpperInvariant()}.",
                ApiErrorCodes.TenantStatusTransitionInvalid);
        }

        var now = DateTimeOffset.UtcNow;
        var effectiveAt = request.EffectiveAt ?? now;
        var previous = tenant.TransitionStatus(target, now);

        // RLS (ADR-004): tenant_status_history/audit_log/domain_event têm a política tenant_isolation
        // — o ator é admin de plataforma sem tenant próprio, mesmo mecanismo de
        // RecordSupportAccessCommandHandler/ProvisionTenantCommandHandler.
        await _db.SetTenantContextAsync(tenant.Id, cancellationToken);

        var statusChangedEvent = DomainEvent.Create(
            tenant.Id,
            type: "tenant.status_changed",
            aggregateType: "tenant",
            aggregateId: tenant.Id,
            payload: JsonSerializer.Serialize(new
            {
                tenantId = tenant.Id,
                previousStatus = previous.ToWireLabel(),
                status = target.ToWireLabel(),
                reason = request.Reason,
                effectiveAt,
                actorId = request.ActorId,
            }),
            origin: "CLOUD",
            occurredAt: now,
            actorId: request.ActorId);
        _db.DomainEvents.Add(statusChangedEvent);

        _db.TenantStatusHistories.Add(TenantStatusHistory.Create(
            tenant.Id,
            previous,
            target,
            request.Reason,
            origin: AdminOrigin,
            effectiveAt: effectiveAt,
            actorId: request.ActorId,
            domainEventId: statusChangedEvent.Id));

        _db.AuditLogs.Add(AuditLog.Create(
            tenant.Id,
            action: "TENANT_STATUS_CHANGED",
            entity: "tenant",
            occurredAt: now,
            actorId: request.ActorId,
            entityId: tenant.Id,
            before: JsonSerializer.Serialize(new { status = previous.ToWireLabel() }),
            after: JsonSerializer.Serialize(new { status = target.ToWireLabel() }),
            reason: request.Reason,
            domainEventId: statusChangedEvent.Id));

        var response = new TenantStatusTransitionResponse(
            tenant.Id, previous.ToWireLabel(), target.ToWireLabel(), tenant.StatusVersion, now);

        return Result<TenantStatusTransitionResponse>.Success(response);
    }
}
