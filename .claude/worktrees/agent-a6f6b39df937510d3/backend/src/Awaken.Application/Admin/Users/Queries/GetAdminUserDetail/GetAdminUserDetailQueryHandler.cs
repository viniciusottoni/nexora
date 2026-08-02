using Awaken.Application.Common.Exceptions;
using Awaken.Contracts.Admin.Users;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Admin.Users.Queries.GetAdminUserDetail;

/// <summary>
/// US-167: handler de detalhe de usuário para o site admin, com dados de subscrição.
/// </summary>
public class GetAdminUserDetailQueryHandler(IAdminUserQueryRepository repo)
    : IRequestHandler<GetAdminUserDetailQuery, AdminUserDetailResponse>
{
    public async Task<AdminUserDetailResponse> Handle(GetAdminUserDetailQuery request, CancellationToken ct)
    {
        var row = await repo.GetByIdAsync(request.UserId, ct)
            ?? throw new NotFoundException("User", request.UserId);

        return new AdminUserDetailResponse(
            row.Id,
            row.Email,
            row.DisplayName,
            row.AvatarUrl,
            row.PreferredLanguage,
            row.IsEmailVerified,
            row.IsOnboardingComplete,
            row.AuthProvider,
            row.LastLoginAtUtc,
            row.TrialEndsAt,
            row.Plan,
            row.SubscriptionStatus,
            row.SubscriptionExpiresAt,
            row.CreatedAtUtc);
    }
}
