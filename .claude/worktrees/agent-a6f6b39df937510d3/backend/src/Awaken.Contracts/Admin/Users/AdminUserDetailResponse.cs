namespace Awaken.Contracts.Admin.Users;

public record AdminUserDetailResponse(
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
    string? Plan,
    string? SubscriptionStatus,
    DateTime? SubscriptionExpiresAt,
    DateTime CreatedAtUtc);
