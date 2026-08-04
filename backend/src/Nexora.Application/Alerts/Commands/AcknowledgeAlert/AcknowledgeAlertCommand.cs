using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Alerts;

namespace Nexora.Application.Alerts.Commands.AcknowledgeAlert;

/// <summary>US-081 §7 <c>POST /v1/alerts/{id}/acknowledge</c> — US-081 §4 "o tempo até o reconhecimento deve ser registrado".</summary>
public sealed record AcknowledgeAlertCommand(Guid AlertId) : ICommand<AlertResponse>;
