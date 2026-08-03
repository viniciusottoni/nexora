using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Contracts.Operation;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tables.Queries.ListTables;

internal sealed class ListTablesQueryHandler : IRequestHandler<ListTablesQuery, Result<TableListResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public ListTablesQueryHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<TableListResponse>> Handle(ListTablesQuery request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result<TableListResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado ao seu usuário.",
                ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;

        var query = _db.DiningTables
            .AsNoTracking()
            .Where(t => t.TenantId == tenantId && t.DeletedAt == null);

        if (request.AreaId is { } areaId)
        {
            query = query.Where(t => t.AreaId == areaId);
        }

        // Materializa antes de converter o enum Status para texto — EF Core não garante tradução
        // de `.ToString()` de enum (mapeado como integer, ver DiningTableConfiguration) para SQL;
        // convertendo depois de ToListAsync, o mapeamento acontece em memória (LINQ-to-Objects).
        var rows = await query
            .OrderBy(t => t.SortOrder).ThenBy(t => t.Label)
            .Select(t => new { t.Id, t.AreaId, AreaName = t.Area.Name, t.Label, t.Seats, t.Status, t.IsActive, t.SortOrder })
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(t => new TableResponse(t.Id, t.AreaId, t.AreaName, t.Label, t.Seats, t.Status.ToString().ToUpperInvariant(), t.IsActive, t.SortOrder))
            .ToList();

        return Result<TableListResponse>.Success(new TableListResponse(items));
    }
}
