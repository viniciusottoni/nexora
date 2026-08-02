namespace Awaken.Contracts.Legal;

public record AcceptResponsibilityNoticeRequest(
    string? NoticeVersion,
    bool? Accepted);
