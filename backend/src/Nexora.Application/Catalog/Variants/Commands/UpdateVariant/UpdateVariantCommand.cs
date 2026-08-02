using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Catalog;

namespace Nexora.Application.Catalog.Variants.Commands.UpdateVariant;

/// <summary>Corpo de <c>PATCH /v1/catalog/variants/{id}</c> — nome, SKU e <c>sizeCode</c> (US-011 §10).</summary>
public sealed record UpdateVariantCommand(Guid VariantId, string Name, string? SizeCode, string? Sku) : ICommand<VariantResponse>;
