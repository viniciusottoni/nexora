using Awaken.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Awaken.Infrastructure.Persistence.Repositories;

/// <summary>
/// EPIC-017 — US-167: repositório de leitura agregada de usuários + assinaturas
/// via LEFT JOIN, exclusivo para o site admin. Não modifica dados.
/// </summary>
public class AdminUserQueryRepository(AwakenDbContext context) : IAdminUserQueryRepository
{
    public async Task<(IReadOnlyList<AdminUserRow> Items, int Total)> GetPagedAsync(
        string? search, string? plan, string? status, int page, int pageSize, CancellationToken ct = default)
    {
        var query = from u in context.Users
                    join s in context.Subscriptions on u.Id equals s.UserId into sj
                    from sub in sj.DefaultIfEmpty()
                    select new
                    {
                        u.Id,
                        u.Email,
                        u.DisplayName,
                        u.AvatarUrl,
                        u.PreferredLanguage,
                        u.IsEmailVerified,
                        u.IsOnboardingComplete,
                        u.Provider,
                        u.LastLoginAtUtc,
                        u.TrialEndsAt,
                        u.CreatedAtUtc,
                        Plan = sub != null ? sub.Plan : null,
                        SubscriptionStatus = sub != null ? sub.Status : null,
                        SubscriptionExpiresAt = sub != null ? sub.ExpiresAt : null,
                    };

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(r =>
                r.Email.Contains(search) ||
                (r.DisplayName != null && r.DisplayName.Contains(search)));

        if (!string.IsNullOrWhiteSpace(plan))
            query = query.Where(r => r.Plan == plan);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(r => r.SubscriptionStatus == status);

        var total = await query.CountAsync(ct);
        var rows = await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = rows.Select(r => new AdminUserRow(
            r.Id, r.Email, r.DisplayName, r.AvatarUrl, r.PreferredLanguage,
            r.IsEmailVerified, r.IsOnboardingComplete, r.Provider.ToString(),
            r.LastLoginAtUtc, r.TrialEndsAt, r.CreatedAtUtc,
            r.Plan, r.SubscriptionStatus, r.SubscriptionExpiresAt)).ToList();

        return (items, total);
    }

    public async Task<AdminUserRow?> GetByIdAsync(Guid userId, CancellationToken ct = default)
    {
        var r = await (from u in context.Users
                       join s in context.Subscriptions on u.Id equals s.UserId into sj
                       from sub in sj.DefaultIfEmpty()
                       where u.Id == userId
                       select new
                       {
                           u.Id,
                           u.Email,
                           u.DisplayName,
                           u.AvatarUrl,
                           u.PreferredLanguage,
                           u.IsEmailVerified,
                           u.IsOnboardingComplete,
                           u.Provider,
                           u.LastLoginAtUtc,
                           u.TrialEndsAt,
                           u.CreatedAtUtc,
                           Plan = sub != null ? sub.Plan : null,
                           SubscriptionStatus = sub != null ? sub.Status : null,
                           SubscriptionExpiresAt = sub != null ? sub.ExpiresAt : null,
                       })
            .FirstOrDefaultAsync(ct);

        if (r is null) return null;

        return new AdminUserRow(
            r.Id, r.Email, r.DisplayName, r.AvatarUrl, r.PreferredLanguage,
            r.IsEmailVerified, r.IsOnboardingComplete, r.Provider.ToString(),
            r.LastLoginAtUtc, r.TrialEndsAt, r.CreatedAtUtc,
            r.Plan, r.SubscriptionStatus, r.SubscriptionExpiresAt);
    }
}
