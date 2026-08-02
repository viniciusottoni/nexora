namespace Awaken.Contracts.Legal;

public record AcceptLegalTermsRequest(
    string? TermsVersion,
    string? PrivacyVersion,
    bool? Accepted);
