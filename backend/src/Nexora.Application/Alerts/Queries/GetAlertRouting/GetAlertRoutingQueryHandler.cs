using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Alerts.Support;
using Nexora.Contracts.Alerts;
using Nexora.Domain.Metrics;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Alerts.Queries.GetAlertRouting;

internal sealed class GetAlertRoutingQueryHandler
    : IRequestHandler<GetAlertRoutingQuery, Result<IReadOnlyDictionary<string, AlertRoutingRuleResponse>>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public GetAlertRoutingQueryHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<IReadOnlyDictionary<string, AlertRoutingRuleResponse>>> Handle(
        GetAlertRoutingQuery request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result<IReadOnlyDictionary<string, AlertRoutingRuleResponse>>.Failure(
                "Não foi possível identificar o estabelecimento vinculado à sua sessão.", ApiErrorCodes.TenantContextMissing);
        }

        var config = await _db.TenantConfigs.AsNoTracking()
            .FirstOrDefaultAsync(c => c.TenantId == _tenantContext.TenantId.Value, cancellationToken);

        var routing = AlertRoutingConfig.Parse(config?.Operation);

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
