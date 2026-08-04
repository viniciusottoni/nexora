using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Alerts.Support;
using Nexora.Contracts.Alerts;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Alerts.Commands.AcknowledgeAlert;

internal sealed class AcknowledgeAlertCommandHandler : IRequestHandler<AcknowledgeAlertCommand, Result<AlertResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public AcknowledgeAlertCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<AlertResponse>> Handle(AcknowledgeAlertCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null || _tenantContext.UserId is null)
        {
            return Result<AlertResponse>.Failure(
                "Não foi possível identificar o usuário vinculado à sua sessão.", ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;

        // RLS (interceptor de conexão) já restringe ao tenant corrente — sem filtro manual de
        // tenant_id aqui (mesmo padrão de GetTableMapQueryHandler, CLAUDE.md/ADR-004); mantido só
        // por defesa em profundidade explícita no filtro (não deixa a query ambígua para quem lê).
        var alert = await _db.Alerts.FirstOrDefaultAsync(
            a => a.Id == request.AlertId && a.TenantId == tenantId, cancellationToken);

        if (alert is null)
        {
            return Result<AlertResponse>.Failure("Alerta não encontrado.", ApiErrorCodes.AlertNotFound);
        }

        if (alert.AcknowledgedAt is null)
        {
            alert.Acknowledge(_tenantContext.UserId.Value);
        }

        return Result<AlertResponse>.Success(AlertMapper.ToResponse(alert));
    }
}
