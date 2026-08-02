namespace Awaken.Contracts.Admin.Security;

/// <summary>US-219: vincula um alerta de segurança a um bug/incidente operacional já existente.</summary>
public record LinkAlertToBugRequest(Guid BugId);
