using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Contracts.Alerts;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Alerts.Commands.SubscribePush;

/// <summary>Reassinar o MESMO endpoint (ex.: reload da página) atualiza as chaves em vez de duplicar — mesmo espírito idempotente de <c>MarkProductUnavailableCommandHandler</c>.</summary>
internal sealed class SubscribePushCommandHandler : IRequestHandler<SubscribePushCommand, Result<SubscribePushResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public SubscribePushCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<SubscribePushResponse>> Handle(SubscribePushCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null || _tenantContext.UserId is null)
        {
            return Result<SubscribePushResponse>.Failure(
                "Não foi possível identificar o usuário vinculado à sua sessão.", ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;

        var existing = await _db.PushSubscriptions.FirstOrDefaultAsync(
            s => s.TenantId == tenantId && s.Endpoint == request.Endpoint, cancellationToken);

        if (existing is not null)
        {
            existing.Touch();
            return Result<SubscribePushResponse>.Success(new SubscribePushResponse(true));
        }

        _db.PushSubscriptions.Add(PushSubscription.Create(
            tenantId, _tenantContext.UserId.Value, request.Endpoint, request.P256dhKey, request.AuthKey));

        return Result<SubscribePushResponse>.Success(new SubscribePushResponse(true));
    }
}
