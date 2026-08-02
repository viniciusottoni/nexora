namespace Awaken.Domain.Repositories;

/// <summary>
/// EPIC-017 — US-167: consulta administrativa paginada de usuários com dados de subscrição
/// via LEFT JOIN. Evita poluir IUserRepository com preocupações administrativas.
/// </summary>
public interface IAdminUserQueryRepository
{
    Task<(IReadOnlyList<AdminUserRow> Items, int Total)> GetPagedAsync(
        string? search, string? plan, string? status, int page, int pageSize, CancellationToken ct = default);

    Task<AdminUserRow?> GetByIdAsync(Guid userId, CancellationToken ct = default);
}

public record AdminUserRow(
    Guid Id,
    string Email,
    string? DisplayName,
    string? AvatarUrl,
    string PreferredLanguage,
    bool IsEmailVerified,
    bool IsOnboardingComplete,
    string AuthProvider,
    DateTime? LastLoginAtUtc,
    DateTime? TrialEndsAt,
    DateTime CreatedAtUtc,
    string? Plan,
    string? SubscriptionStatus,
    DateTime? SubscriptionExpiresAt);
