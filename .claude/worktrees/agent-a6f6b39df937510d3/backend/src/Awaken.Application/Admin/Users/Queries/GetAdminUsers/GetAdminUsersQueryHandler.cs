using Awaken.Contracts.Admin.Users;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Admin.Users.Queries.GetAdminUsers;

/// <summary>
/// US-167: handler de listagem paginada de usuários para o site admin.
/// Delega ao IAdminUserQueryRepository que faz LEFT JOIN com Subscriptions.
/// </summary>
public class GetAdminUsersQueryHandler(IAdminUserQueryRepository repo)
    : IRequestHandler<GetAdminUsersQuery, AdminUserListResponse>
{
    public async Task<AdminUserListResponse> Handle(GetAdminUsersQuery request, CancellationToken ct)
    {
        var (items, total) = await repo.GetPagedAsync(
            request.Search,
            request.Plan,
            request.Status,
            request.Page,
            request.PageSize,
            ct);

        var projected = items
            .Select(r => new AdminUserSummaryResponse(
                r.Id,
                r.Email,
                r.DisplayName,
                r.Plan,
                r.SubscriptionStatus,
                r.IsEmailVerified,
                r.IsOnboardingComplete,
                r.LastLoginAtUtc,
                r.CreatedAtUtc))
            .ToList();

        return new AdminUserListResponse(projected, total, request.Page, request.PageSize);
    }
}
