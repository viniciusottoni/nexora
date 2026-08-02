namespace Awaken.Contracts.Users;

public record OnboardingStatusResponse(
    bool OnboardingCompleted,
    string CurrentStep);
