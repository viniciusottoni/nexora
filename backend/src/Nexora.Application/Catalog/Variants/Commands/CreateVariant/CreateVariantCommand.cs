using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Catalog;

namespace Nexora.Application.Catalog.Variants.Commands.CreateVariant;

/// <summary>
/// Cria uma variante (tamanho) de um produto já existente, junto com seu preço base em um único
/// canal (US-011 §7). Porta de <c>POST /v1/catalog/products/{productId}/variants</c>.
/// </summary>
public sealed record CreateVariantCommand(
    Guid ProductId,
    string Name,
    string? SizeCode,
    string? Sku,
    short? PrepMinutes,
    bool IsDefault,
    decimal BasePrice,
    string? Channel) : ICommand<VariantResponse>;
