namespace Awaken.Contracts.Auth;

public record SessionResponse(
    Guid UserId,
    bool IsAuthenticated,
    string AccessStatus,
    bool OnboardingCompleted);
