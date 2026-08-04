using System.Globalization;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Alerts.Support;
using Nexora.Contracts.Alerts;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Alerts.Queries.GetTenantThresholds;

internal sealed class GetTenantThresholdsQueryHandler : IRequestHandler<GetTenantThresholdsQuery, Result<TenantThresholdsResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public GetTenantThresholdsQueryHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<TenantThresholdsResponse>> Handle(GetTenantThresholdsQuery request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result<TenantThresholdsResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado à sua sessão.", ApiErrorCodes.TenantContextMissing);
        }

        var config = await _db.TenantConfigs.AsNoTracking()
            .FirstOrDefaultAsync(c => c.TenantId == _tenantContext.TenantId.Value, cancellationToken);

        // Tenant sem TenantConfig (não deveria acontecer em produção — toda criação de tenant
        // provisiona a config, ver ProvisionTenantCommandHandler) recebe os padrões de mesmo jeito,
        // em vez de erro: US-080 §10 "funcionar sem configurar nada".
        var thresholds = AlertThresholds.Parse(config?.Thresholds);

        return Result<TenantThresholdsResponse>.Success(new TenantThresholdsResponse(
            thresholds.OrderWarnMinutes,
            thresholds.OrderCriticalMinutes,
            thresholds.ItemInWindowMinutes,
            thresholds.TableIdleMinutes,
            thresholds.CashDivergenceAlert.ToString(CultureInfo.InvariantCulture),
            thresholds.CmvDivergencePercent,
            thresholds.SyncDelayMinutes,
            thresholds.DineInPromiseMinutes,
            thresholds.DeliveryPromiseMinutes,
            thresholds.AvgTimeAboveTargetPercent,
            thresholds.CancellationCountThreshold,
            thresholds.CancellationWindowMinutes,
            thresholds.DiscountAboveThresholdPercent,
            thresholds.DiscountWindowMinutes));
    }
}
