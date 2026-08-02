namespace Awaken.Contracts.Support;

public record CreateSupportTicketRequest(
    string Category,
    string Description,
    string? AppVersion,
    string? CorrelationId);
