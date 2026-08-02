using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Contracts.Tenants;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tenants.Queries.GetTenantById;

internal sealed class GetTenantByIdQueryHandler : IRequestHandler<GetTenantByIdQuery, Result<TenantSummaryResponse>>
{
    private readonly IApplicationDbContext _db;

    public GetTenantByIdQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<TenantSummaryResponse>> Handle(GetTenantByIdQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _db.Tenants
            .AsNoTracking()
            .Where(t => t.Id == request.TenantId && t.DeletedAt == null)
            .Select(t => new TenantSummaryResponse(t.Id, t.Slug, t.Name, t.Plan, t.Status.ToString(), t.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (tenant is null)
        {
            return Result<TenantSummaryResponse>.Failure("Estabelecimento não encontrado.", ApiErrorCodes.TenantNotFound);
        }

        return Result<TenantSummaryResponse>.Success(tenant);
    }
}
