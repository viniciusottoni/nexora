namespace Awaken.Contracts.Auth;

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAtUtc,
    UserDto User);

public record UserDto(
    Guid Id,
    string Email,
    string? DisplayName,
    string? AvatarUrl,
    string PreferredLanguage,
    bool IsOnboardingComplete,
    string? AccessStatus = null);
