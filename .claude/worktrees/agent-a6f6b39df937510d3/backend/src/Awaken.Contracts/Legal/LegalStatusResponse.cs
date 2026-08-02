namespace Awaken.Contracts.Legal;

public record LegalStatusResponse(
    bool HasAcceptedLegal,
    bool HasAcceptedResponsibilityNotice,
    string? TermsVersion,
    string? PrivacyVersion,
    string? ResponsibilityNoticeVersion,
    DateTime? TermsAcceptedAt,
    DateTime? PrivacyAcceptedAt,
    DateTime? ResponsibilityNoticeAcceptedAt);
