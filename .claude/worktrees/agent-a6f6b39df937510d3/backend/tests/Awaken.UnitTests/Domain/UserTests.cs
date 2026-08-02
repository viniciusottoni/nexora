using FluentAssertions;
using Awaken.Domain.Entities.Auth;

namespace Awaken.UnitTests.Domain;

public class UserTests
{
    [Fact]
    public void CreateNormalizesEmailAndUsesPortugueseAsDefaultLanguage()
    {
        var user = User.Create("Hunter@Awaken.App", "hashed-password", "Hunter");

        user.Email.Should().Be("hunter@awaken.app");
        user.PasswordHash.Should().Be("hashed-password");
        user.DisplayName.Should().Be("Hunter");
        user.PreferredLanguage.Should().Be("pt-BR");
        user.IsOnboardingComplete.Should().BeFalse();
    }

    [Fact]
    public void CreateUsesProvidedPreferredLanguageWhenInformed()
    {
        var user = User.Create("hunter@awaken.app", "hashed-password", "Hunter", "en");

        user.PreferredLanguage.Should().Be("en");
    }

    [Fact]
    public void CreateFromGoogleNormalizesEmailAndMarksProviderAsVerified()
    {
        var user = User.CreateFromGoogle("Hunter@Awaken.App", "google-sub-123", "Hunter", "https://avatar.url");

        user.Email.Should().Be("hunter@awaken.app");
        user.PasswordHash.Should().BeNull();
        user.Provider.Should().Be(AuthProvider.Google);
        user.ProviderUserId.Should().Be("google-sub-123");
        user.DisplayName.Should().Be("Hunter");
        user.AvatarUrl.Should().Be("https://avatar.url");
        user.IsEmailVerified.Should().BeTrue();
        user.PreferredLanguage.Should().Be("pt-BR");
    }

    [Fact]
    public void LinkGoogleProviderUpdatesProviderAndProviderUserId()
    {
        var user = User.Create("hunter@awaken.app", "hashed-password", "Hunter");
        var now = new DateTime(2026, 6, 17, 0, 0, 0, DateTimeKind.Utc);

        user.LinkGoogleProvider("google-sub-456", now);

        user.Provider.Should().Be(AuthProvider.Google);
        user.ProviderUserId.Should().Be("google-sub-456");
        user.IsEmailVerified.Should().BeTrue();
        user.UpdatedAtUtc.Should().Be(now);
    }

    [Fact]
    public void ComputeAccessStatusReturnsNoTrialWhenTrialEndsAtIsNull()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        var utcNow = new DateTime(2026, 6, 18, 0, 0, 0, DateTimeKind.Utc);

        user.ComputeAccessStatus(utcNow).Should().Be("no_trial");
    }

    [Fact]
    public void ComputeAccessStatusReturnsTrialActiveWhenTrialIsOngoing()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        var utcNow = new DateTime(2026, 6, 18, 0, 0, 0, DateTimeKind.Utc);
        user.StartTrial(utcNow.AddDays(7));

        user.ComputeAccessStatus(utcNow).Should().Be("trial_active");
    }

    [Fact]
    public void ComputeAccessStatusReturnsTrialExpiredWhenTrialEnded()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        var utcNow = new DateTime(2026, 6, 25, 0, 0, 0, DateTimeKind.Utc);
        user.StartTrial(new DateTime(2026, 6, 18, 0, 0, 0, DateTimeKind.Utc));

        user.ComputeAccessStatus(utcNow).Should().Be("trial_expired");
    }

    [Fact]
    public void StartTrialSetsTrialEndsAt()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        var trialEndsAt = new DateTime(2026, 6, 25, 10, 0, 0, DateTimeKind.Utc);

        user.StartTrial(trialEndsAt);

        user.TrialEndsAt.Should().Be(trialEndsAt);
    }

    [Fact]
    public void SelectAvatarSetsSelectedAvatarKeyAndUpdatedAtUtc()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        var utcNow = new DateTime(2026, 7, 2, 10, 0, 0, DateTimeKind.Utc);

        user.SelectAvatar("avatar_male_3", utcNow);

        user.SelectedAvatarKey.Should().Be("avatar_male_3");
        user.UpdatedAtUtc.Should().Be(utcNow);
    }

    [Fact]
    public void CompleteOnboardingMarksLifecycleTimestampsAndCurrentStep()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");

        user.CompleteOnboarding(DateTime.UtcNow);

        user.IsOnboardingComplete.Should().BeTrue();
        user.OnboardingStartedAtUtc.Should().NotBeNull();
        user.OnboardingCompletedAtUtc.Should().NotBeNull();
        user.CurrentOnboardingStep.Should().Be("completed");
    }
}
