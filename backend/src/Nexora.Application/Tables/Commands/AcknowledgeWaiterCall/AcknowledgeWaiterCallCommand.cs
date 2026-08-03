using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Operation;

namespace Nexora.Application.Tables.Commands.AcknowledgeWaiterCall;

/// <summary>
/// Porta de <c>POST /v1/tables/{id}/acknowledge-call</c> (US-025 §7) — o garçom confirma que
/// atendeu a chamada, resolvendo o <c>Alert</c> pendente da mesa (cenário Gherkin "Confirmação de
/// atendimento": "o indicador deve desaparecer do mapa" e "o cliente deve ver que a chamada foi
/// atendida").
/// </summary>
public sealed record AcknowledgeWaiterCallCommand(Guid TableId) : ICommand<AcknowledgeWaiterCallResponse>;
