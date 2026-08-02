using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Platform;
using Nexora.Contracts.Installations;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Installations.Queries.GetInstallationSyncHealth;

internal sealed class GetInstallationSyncHealthQueryHandler
    : IRequestHandler<GetInstallationSyncHealthQuery, Result<InstallationSyncHealthResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IAppVersionProvider _version;

    public GetInstallationSyncHealthQueryHandler(IApplicationDbContext db, IAppVersionProvider version)
    {
        _db = db;
        _version = version;
    }

    public async Task<Result<InstallationSyncHealthResponse>> Handle(
        GetInstallationSyncHealthQuery request, CancellationToken cancellationToken)
    {
        var config = await _db.TenantConfigs.AsNoTracking()
            .FirstOrDefaultAsync(c => c.TenantId == request.TenantId, cancellationToken);

        if (config is null)
        {
            return Result<InstallationSyncHealthResponse>.Failure(
                "Instalação não encontrada.", ApiErrorCodes.InstallationNotFound);
        }

        return Result<InstallationSyncHealthResponse>.Success(new InstallationSyncHealthResponse(
            DateTimeOffset.UtcNow, _version.CurrentVersion, config.ConfigVersion));
    }
}
