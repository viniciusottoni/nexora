using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Admin.MvpHealth;
using MediatR;

namespace Awaken.Application.Admin.MvpHealth.Queries.GetMvpHealth;

/// <summary>
/// US-216: handler que delega ao IMvpHealthService a agregação de sinais de todos os domínios operacionais.
/// </summary>
public class GetMvpHealthQueryHandler(IMvpHealthService mvpHealthService)
    : IRequestHandler<GetMvpHealthQuery, MvpHealthStatusResponse>
{
    public Task<MvpHealthStatusResponse> Handle(GetMvpHealthQuery request, CancellationToken cancellationToken) =>
        mvpHealthService.GetMvpHealthAsync(cancellationToken);
}
