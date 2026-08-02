using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Catalog;

namespace Nexora.Application.Catalog.Products.Commands.UpdateProduct;

/// <summary>
/// Atualiza os campos de cadastro de um produto do tenant autenticado — todos opcionais, só o
/// que for enviado é alterado (mesmo padrão de <c>UpdateStationCommand</c>/<c>UpdateBrandingCommand</c>:
/// nulo significa "não alterar", nunca "limpar"). <see cref="StationId"/> nulo mantém a praça atual —
/// para desvincular o produto de qualquer praça, o valor precisa ser <see cref="Guid.Empty"/>
/// (sentinela reservado, documentado em <c>UpdateProductCommandHandler</c>), já que o restante do
/// código-base não tem uma convenção de "limpar campo opcional via PATCH". Não inclui
/// <c>isActive</c>: ativação/desativação têm comandos e endpoints dedicados (US-010 §3.1). Porta
/// de <c>PATCH /v1/catalog/products/:id</c>.
/// </summary>
public sealed record UpdateProductCommand(
    Guid ProductId,
    string? Name,
    Guid? CategoryId,
    Guid? StationId,
    string? Description,
    string? IngredientsText,
    IReadOnlyList<string>? Allergens,
    bool? AllowsFractions,
    short? MaxFractions,
    short? Position) : ICommand<ProductResponse>;
