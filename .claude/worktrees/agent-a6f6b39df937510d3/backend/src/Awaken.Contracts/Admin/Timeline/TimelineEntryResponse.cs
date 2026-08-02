namespace Awaken.Contracts.Admin.Timeline;

public record TimelineEntryResponse(
    string Id,
    string EntryType,
    string Title,
    string Description,
    string Severity,
    DateTime OccurredAtUtc,
    string? MaskedUserId,
    string? ResourceId,
    string? ResourceType,
    string? CorrelationId,
    bool IsRelationCertain,
    string? DetailUrl);
