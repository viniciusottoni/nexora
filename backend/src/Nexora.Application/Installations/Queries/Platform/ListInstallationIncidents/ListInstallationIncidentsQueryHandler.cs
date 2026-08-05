using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Installations.Support;
using Nexora.Contracts.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Installations.Queries.Platform.ListInstallationIncidents;

/// <summary>US-140 §4 "Histórico de incidentes" — duração e causa de cada incidente, mais recente primeiro.</summary>
internal sealed class ListInstallationIncidentsQueryHandler
    : IRequestHandler<ListInstallationIncidentsQuery, Result<InstallationIncidentListResponse>>
{
    private readonly IApplicationDbContext _db;

    public ListInstallationIncidentsQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<InstallationIncidentListResponse>> Handle(
        ListInstallationIncidentsQuery request, CancellationToken cancellationToken)
    {
        var installation = await PlatformInstallationLookup.FindAsync(_db, request.InstallationId, cancellationToken);
        if (installation is null)
        {
            return Result<InstallationIncidentListResponse>.Failure(
                "Instalação não encontrada.", ApiErrorCodes.InstallationNotFound);
        }

        await _db.SetTenantContextAsync(installation.TenantId, cancellationToken);

        var now = DateTimeOffset.UtcNow;

        var incidents = await _db.InstallationIncidents.AsNoTracking()
            .Where(i => i.InstallationId == installation.Id)
            .OrderByDescending(i => i.StartedAt)
            .ToListAsync(cancellationToken);

        var response = incidents
            .Select(incident => new InstallationIncidentResponse(
                incident.Id,
                incident.Type.ToString().ToUpperInvariant(),
                incident.StartedAt,
                incident.ResolvedAt,
                incident.Cause,
                (long)((incident.ResolvedAt ?? now) - incident.StartedAt).TotalSeconds))
            .ToList();

        return Result<InstallationIncidentListResponse>.Success(new InstallationIncidentListResponse(response));
    }
}
