using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Alerts.Support;
using Nexora.Contracts.Alerts;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Alerts.Commands.ResolveAlert;

internal sealed class ResolveAlertCommandHandler : IRequestHandler<ResolveAlertCommand, Result<AlertResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly IAlertsBroadcaster _broadcaster;

    public ResolveAlertCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext, IAlertsBroadcaster broadcaster)
    {
        _db = db;
        _tenantContext = tenantContext;
        _broadcaster = broadcaster;
    }

    public async Task<Result<AlertResponse>> Handle(ResolveAlertCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result<AlertResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado à sua sessão.", ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;

        var alert = await _db.Alerts.FirstOrDefaultAsync(
            a => a.Id == request.AlertId && a.TenantId == tenantId, cancellationToken);

        if (alert is null)
        {
            return Result<AlertResponse>.Failure("Alerta não encontrado.", ApiErrorCodes.AlertNotFound);
        }

        if (alert.ResolvedAt is not null)
        {
            return Result<AlertResponse>.Failure("Este alerta já foi resolvido.", ApiErrorCodes.AlertAlreadyResolved);
        }

        alert.Resolve();
        await _broadcaster.AlertResolved(alert, cancellationToken);

        return Result<AlertResponse>.Success(AlertMapper.ToResponse(alert));
    }
}
