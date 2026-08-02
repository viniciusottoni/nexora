using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Catalog;

namespace Nexora.Application.Catalog.Variants.Queries.ListVariantsForProduct;

/// <summary>
/// Porta de <c>GET /v1/catalog/products/{productId}/variants</c> — lista as variantes de um
/// produto (ativas e inativas) com o preço vigente de cada uma em <see cref="Channel"/> (padrão
/// <c>DineIn</c>). Extensão além do contrato literal do §7 da US-011: necessária para a tela de
/// gestão exibir/editar as variações em linha na mesma tela do produto (US-011 §10) — ver
/// relatório da tarefa.
/// </summary>
public sealed record ListVariantsForProductQuery(Guid ProductId, string? Channel) : IQuery<VariantListResponse>;
