using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Catalog;

namespace Nexora.Application.Catalog.Variants.Queries.GetVariant;

/// <summary>Porta de <c>GET /v1/catalog/variants/{id}</c> (US-011 §7). <see cref="Channel"/> opcional — padrão <c>DineIn</c> quando nulo.</summary>
public sealed record GetVariantQuery(Guid VariantId, string? Channel) : IQuery<VariantResponse>;
