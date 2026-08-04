using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Alerts;

namespace Nexora.Application.Alerts.Commands.SubscribePush;

/// <summary>
/// US-081 §7 <c>POST /v1/notifications/subscribe</c> — sempre na nuvem (US-081 §2: "o push é
/// enviado pela nuvem"), mesmo quando o navegador que se inscreve está operando contra o edge.
/// </summary>
public sealed record SubscribePushCommand(string Endpoint, string P256dhKey, string AuthKey) : ICommand<SubscribePushResponse>;
