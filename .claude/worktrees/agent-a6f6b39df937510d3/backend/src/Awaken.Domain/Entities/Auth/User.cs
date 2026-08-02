using Awaken.Domain.Common;

namespace Awaken.Domain.Entities.Auth;

public class User : BaseEntity
{
    public string Email { get; private set; } = string.Empty;
    public string? PasswordHash { get; private set; }
    public string? DisplayName { get; private set; }
    public string? AvatarUrl { get; private set; }
    public string? SelectedAvatarKey { get; private set; }
    public string PreferredLanguage { get; private set; } = "pt-BR";
    public bool IsOnboardingComplete { get; private set; }
    public DateTime? OnboardingStartedAtUtc { get; private set; }
    public DateTime? OnboardingCompletedAtUtc { get; private set; }
    public string? CurrentOnboardingStep { get; private set; }
    public bool IsEmailVerified { get; private set; }
    public DateTime? LastLoginAtUtc { get; private set; }
    public AuthProvider Provider { get; private set; } = AuthProvider.Local;
    public string? ProviderUserId { get; private set; }
    public DateTime? TrialEndsAt { get; private set; }
    public DateTime? TermsAcceptedAt { get; private set; }
    public DateTime? PrivacyAcceptedAt { get; private set; }
    public string? TermsVersion { get; private set; }
    public string? PrivacyVersion { get; private set; }
    public DateTime? ResponsibilityNoticeAcceptedAt { get; private set; }
    public string? ResponsibilityNoticeVersion { get; private set; }
    public string? Role { get; private set; }

    public bool HasAcceptedLegal => TermsAcceptedAt != null && PrivacyAcceptedAt != null;
    public bool HasAcceptedResponsibilityNotice => ResponsibilityNoticeAcceptedAt != null;

    private User() { }

    public static User Create(
        string email,
        string passwordHash,
        string? displayName = null,
        string preferredLanguage = "pt-BR")
    {
        return new User
        {
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            DisplayName = displayName,
            PreferredLanguage = preferredLanguage,
            Provider = AuthProvider.Local,
        };
    }

    public static User CreateFromGoogle(
        string email,
        string providerUserId,
        string? displayName = null,
        string? avatarUrl = null,
        string preferredLanguage = "pt-BR")
    {
        return new User
        {
            Email = email.ToLowerInvariant(),
            PasswordHash = null,
            DisplayName = displayName,
            AvatarUrl = avatarUrl,
            PreferredLanguage = preferredLanguage,
            Provider = AuthProvider.Google,
            ProviderUserId = providerUserId,
            IsEmailVerified = true,
        };
    }

    public void LinkGoogleProvider(string providerUserId, DateTime utcNow)
    {
        Provider = AuthProvider.Google;
        ProviderUserId = providerUserId;
        IsEmailVerified = true;
        UpdatedAtUtc = utcNow;
    }

    public void UpdatePreferredLanguage(string language, DateTime utcNow)
    {
        PreferredLanguage = language;
        UpdatedAtUtc = utcNow;
    }

    public void CompleteOnboarding(DateTime utcNow)
    {
        OnboardingStartedAtUtc ??= utcNow;
        OnboardingCompletedAtUtc = utcNow;
        CurrentOnboardingStep = "completed";
        IsOnboardingComplete = true;
        UpdatedAtUtc = utcNow;
    }

    public void RecordLogin(DateTime utcNow)
    {
        LastLoginAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void UpdateProfile(string? displayName, string? avatarUrl, DateTime utcNow)
    {
        DisplayName = displayName;
        AvatarUrl = avatarUrl;
        UpdatedAtUtc = utcNow;
    }

    /// US-230: renomeia o hunter via Pergaminho de Renomeação. Método dedicado
    /// — NÃO reaproveitar UpdateProfile aqui: aquele método zera AvatarUrl
    /// incondicionalmente, o que apagaria o avatar do Google num rename.
    public void Rename(string displayName, DateTime utcNow)
    {
        DisplayName = displayName;
        UpdatedAtUtc = utcNow;
    }

    /// US-234: seleciona um avatar do catalogo interno (RN-003). Ate a
    /// primeira selecao manual, SelectedAvatarKey permanece null e a
    /// apresentacao decide o fallback (AvatarUrl do Google > avatar padrao -
    /// RN-001/RN-002). Uma vez selecionado, prevalece sobre AvatarUrl.
    public void SelectAvatar(string avatarKey, DateTime utcNow)
    {
        SelectedAvatarKey = avatarKey;
        UpdatedAtUtc = utcNow;
    }

    public void StartTrial(DateTime trialEndsAtUtc)
    {
        TrialEndsAt = trialEndsAtUtc;
    }

    public void AcceptLegalTerms(string termsVersion, string privacyVersion, DateTime utcNow)
    {
        TermsVersion = termsVersion;
        PrivacyVersion = privacyVersion;
        TermsAcceptedAt = utcNow;
        PrivacyAcceptedAt = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void AcceptResponsibilityNotice(string noticeVersion, DateTime utcNow)
    {
        ResponsibilityNoticeVersion = noticeVersion;
        ResponsibilityNoticeAcceptedAt = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void AssignRole(string role, DateTime utcNow)
    {
        Role = role;
        UpdatedAtUtc = utcNow;
    }

    public string ComputeAccessStatus(DateTime utcNow) =>
        TrialEndsAt == null ? "no_trial" :
        TrialEndsAt > utcNow ? "trial_active" : "trial_expired";
}
