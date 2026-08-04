using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Alerts;

namespace Nexora.Application.Alerts.Commands.ResolveAlert;

/// <summary>US-080 §7 <c>POST /v1/alerts/{id}/resolve</c> — encerramento manual (o motor também resolve automaticamente, ver <c>IAlertRaiser.ResolveAsync</c>).</summary>
public sealed record ResolveAlertCommand(Guid AlertId) : ICommand<AlertResponse>;
