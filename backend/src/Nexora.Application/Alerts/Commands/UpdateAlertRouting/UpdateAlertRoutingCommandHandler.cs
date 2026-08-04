using System.Text.Json;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Alerts.Support;
using Nexora.Contracts.Alerts;
using Nexora.Domain.Metrics;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Alerts.Commands.UpdateAlertRouting;

internal sealed class UpdateAlertRoutingCommandHandler
    : IRequestHandler<UpdateAlertRoutingCommand, Result<IReadOnlyDictionary<string, AlertRoutingRuleResponse>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly IEventOriginProvider _eventOrigin;

    public UpdateAlertRoutingCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext, IEventOriginProvider eventOrigin)
    {
        _db = db;
        _tenantContext = tenantContext;
        _eventOrigin = eventOrigin;
    }

    public async Task<Result<IReadOnlyDictionary<string, AlertRoutingRuleResponse>>> Handle(
        UpdateAlertRoutingCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result<IReadOnlyDictionary<string, AlertRoutingRuleResponse>>.Failure(
                "Não foi possível identificar o estabelecimento vinculado à sua sessão.", ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;

        var config = await _db.TenantConfigs.FirstOrDefaultAsync(c => c.TenantId == tenantId, cancellationToken);
        if (config is null)
        {
            return Result<IReadOnlyDictionary<string, AlertRoutingRuleResponse>>.Failure(
                "Estabelecimento não encontrado.", ApiErrorCodes.TenantNotFound);
        }

        var updatedOperationJson = AlertRoutingConfig.ApplyPatch(config.Operation, request.Patch);
        config.UpdateOperation(updatedOperationJson);

        _db.DomainEvents.Add(DomainEvent.Create(
            tenantId,
            type: "tenant.config_updated",
            aggregateType: "tenant_config",
            aggregateId: tenantId,
            payload: JsonSerializer.Serialize(new { section = "alertRouting", types = request.Patch.Keys }),
            origin: _eventOrigin.Origin,
            occurredAt: DateTimeOffset.UtcNow,
            actorId: _tenantContext.UserId,
            deviceId: _tenantContext.DeviceId));

        var routing = AlertRoutingConfig.Parse(updatedOperationJson);
        var result = AlertTypes.EngineTypes.ToDictionary(
            type => type,
            type =>
            {
                var rule = routing.Resolve(type);
                return new AlertRoutingRuleResponse(rule.Roles, rule.Scope, rule.EscalateAfterSeconds, rule.GroupWindowSeconds);
            });

        return Result<IReadOnlyDictionary<string, AlertRoutingRuleResponse>>.Success(result);
    }
}
