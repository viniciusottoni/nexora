using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Catalog;

namespace Nexora.Application.Catalog.Variants.Commands.MarkVariantAsDefault;

/// <summary>
/// Porta de <c>POST /v1/catalog/variants/{id}/mark-default</c> — marca a variante como padrão do
/// produto (pré-selecionada no cardápio) e desmarca qualquer outra variante padrão do mesmo
/// produto, já que só pode existir uma (US-011 §3.1).
/// </summary>
public sealed record MarkVariantAsDefaultCommand(Guid VariantId) : ICommand<VariantResponse>;
