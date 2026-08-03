namespace Nexora.Contracts.Tenants;

/// <summary>Corpo de <c>POST /v1/platform/tenants/{id}/support-access</c> (E-09/US-090, EVT-074).</summary>
public sealed record RecordSupportAccessRequest(string Reason, int DurationMinutes);
