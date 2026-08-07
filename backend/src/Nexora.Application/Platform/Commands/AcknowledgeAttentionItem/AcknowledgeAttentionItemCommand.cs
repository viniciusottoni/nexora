using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Platform;

namespace Nexora.Application.Platform.Commands.AcknowledgeAttentionItem;

/// <summary>US-157 §"Contrato de API" — <c>POST /v1/platform/attention/{itemId}/acknowledgements</c>. RN-004: cria um registro PRÓPRIO, nunca apaga/edita o fato original.</summary>
public sealed record AcknowledgeAttentionItemCommand(
    string ItemId,
    string Reason,
    Guid? ActorId) : ICommand<AttentionAcknowledgementResponse>;
