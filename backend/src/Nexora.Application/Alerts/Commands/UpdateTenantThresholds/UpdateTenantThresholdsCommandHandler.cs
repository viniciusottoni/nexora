using System.Globalization;
using System.Text.Json;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Alerts.Support;
using Nexora.Contracts.Alerts;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Alerts.Commands.UpdateTenantThresholds;

internal sealed class UpdateTenantThresholdsCommandHandler : IRequestHandler<UpdateTenantThresholdsCommand, Result<TenantThresholdsResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly IEventOriginProvider _eventOrigin;

    public UpdateTenantThresholdsCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext, IEventOriginProvider eventOrigin)
    {
        _db = db;
        _tenantContext = tenantContext;
        _eventOrigin = eventOrigin;
    }

    public async Task<Result<TenantThresholdsResponse>> Handle(UpdateTenantThresholdsCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result<TenantThresholdsResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado à sua sessão.", ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;

        var config = await _db.TenantConfigs.FirstOrDefaultAsync(c => c.TenantId == tenantId, cancellationToken);
        if (config is null)
        {
            return Result<TenantThresholdsResponse>.Failure("Estabelecimento não encontrado.", ApiErrorCodes.TenantNotFound);
        }

        var current = AlertThresholds.Parse(config.Thresholds);
        var body = request.Request;

        var merged = current with
        {
            OrderWarnMinutes = body.OrderWarnMinutes ?? current.OrderWarnMinutes,
            OrderCriticalMinutes = body.OrderCriticalMinutes ?? current.OrderCriticalMinutes,
            ItemInWindowMinutes = body.ItemInWindowMinutes ?? current.ItemInWindowMinutes,
            TableIdleMinutes = body.TableIdleMinutes ?? current.TableIdleMinutes,
            CashDivergenceAlert = body.CashDivergenceAlert is { } cash
                ? decimal.Parse(cash, CultureInfo.InvariantCulture)
                : current.CashDivergenceAlert,
            CmvDivergencePercent = body.CmvDivergencePercent ?? current.CmvDivergencePercent,
            SyncDelayMinutes = body.SyncDelayMinutes ?? current.SyncDelayMinutes,
            DineInPromiseMinutes = body.DineInPromiseMinutes ?? current.DineInPromiseMinutes,
            DeliveryPromiseMinutes = body.DeliveryPromiseMinutes ?? current.DeliveryPromiseMinutes,
            AvgTimeAboveTargetPercent = body.AvgTimeAboveTargetPercent ?? current.AvgTimeAboveTargetPercent,
            CancellationCountThreshold = body.CancellationCountThreshold ?? current.CancellationCountThreshold,
            CancellationWindowMinutes = body.CancellationWindowMinutes ?? current.CancellationWindowMinutes,
            DiscountAboveThresholdPercent = body.DiscountAboveThresholdPercent ?? current.DiscountAboveThresholdPercent,
            DiscountWindowMinutes = body.DiscountWindowMinutes ?? current.DiscountWindowMinutes,
        };

        config.UpdateThresholds(merged.ToJson());

        // Mesmo sinal que outras mudanças de tenant_config já emitem (ver CreateTableCommandHandler)
        // — o pull de configuração do edge (US-063) usa isto para saber que há novidade a buscar.
        _db.DomainEvents.Add(DomainEvent.Create(
            tenantId,
            type: "tenant.config_updated",
            aggregateType: "tenant_config",
            aggregateId: tenantId,
            payload: JsonSerializer.Serialize(new { section = "thresholds" }),
            origin: _eventOrigin.Origin,
            occurredAt: DateTimeOffset.UtcNow,
            actorId: _tenantContext.UserId,
            deviceId: _tenantContext.DeviceId));

        return Result<TenantThresholdsResponse>.Success(new TenantThresholdsResponse(
            merged.OrderWarnMinutes,
            merged.OrderCriticalMinutes,
            merged.ItemInWindowMinutes,
            merged.TableIdleMinutes,
            merged.CashDivergenceAlert.ToString(CultureInfo.InvariantCulture),
            merged.CmvDivergencePercent,
            merged.SyncDelayMinutes,
            merged.DineInPromiseMinutes,
            merged.DeliveryPromiseMinutes,
            merged.AvgTimeAboveTargetPercent,
            merged.CancellationCountThreshold,
            merged.CancellationWindowMinutes,
            merged.DiscountAboveThresholdPercent,
            merged.DiscountWindowMinutes));
    }
}
