namespace Awaken.Contracts.Admin.Tickets;

public record TriageTicketRequest(string? Status, string? Priority, Guid? AssignedAdminId);
