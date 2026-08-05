using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Contracts.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tenants.Queries.GetSupportAccessHistory;

internal sealed class GetSupportAccessHistoryQueryHandler
    : IRequestHandler<GetSupportAccessHistoryQuery, Result<SupportAccessListResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public GetSupportAccessHistoryQueryHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<SupportAccessListResponse>> Handle(
        GetSupportAccessHistoryQuery request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is not { } tenantId)
        {
            return Result<SupportAccessListResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado ao seu usuário.",
                ApiErrorCodes.TenantContextMissing);
        }

        var now = DateTimeOffset.UtcNow;

        var rows = await _db.SupportAccesses
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId)
            .OrderByDescending(a => a.GrantedAt)
            .Select(a => new SupportAccessSummaryResponse(
                a.Id,
                a.TenantId,
                null,
                a.GrantedTo,
                a.Reason,
                a.DurationMinutes,
                a.GrantedAt,
                a.ExpiresAt,
                a.RevokedAt,
                a.RevokedBy,
                a.LastUsedAt,
                a.RevokedAt == null && a.ExpiresAt > now))
            .ToListAsync(cancellationToken);

        return Result<SupportAccessListResponse>.Success(new SupportAccessListResponse(rows));
    }
}
