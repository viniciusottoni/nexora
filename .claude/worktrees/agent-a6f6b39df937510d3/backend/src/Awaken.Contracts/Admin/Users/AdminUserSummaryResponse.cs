namespace Awaken.Contracts.Admin.Users;

public record AdminUserSummaryResponse(
    Guid Id,
    string Email,
    string? DisplayName,
    string? Plan,
    string? SubscriptionStatus,
    bool IsEmailVerified,
    bool IsOnboardingComplete,
    DateTime? LastLoginAtUtc,
    DateTime CreatedAtUtc);
