using Awaken.Contracts.Progression;

namespace Awaken.Contracts.Onboarding;

public record CompleteOnboardingResponse(
    bool OnboardingCompleted,
    string NextRoute,
    string Rank,
    int RankScore,
    int Level,
    AttributesDto Attributes,
    bool RankCapApplied);
