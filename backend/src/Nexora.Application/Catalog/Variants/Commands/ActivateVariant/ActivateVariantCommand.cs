using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Catalog;

namespace Nexora.Application.Catalog.Variants.Commands.ActivateVariant;

/// <summary>Porta de <c>POST /v1/catalog/variants/{id}/activate</c> — volta a exibir a variante nos canais de venda.</summary>
public sealed record ActivateVariantCommand(Guid VariantId) : ICommand<VariantResponse>;
