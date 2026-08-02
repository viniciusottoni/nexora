using Nexora.Application.Abstractions.Messaging;

namespace Nexora.Application.Catalog.Categories.Commands.DeactivateCategory;

/// <summary>
/// Desativa (nunca exclui fisicamente) uma categoria do cardápio do tenant autenticado — some dos
/// canais de venda, mas produtos e pedidos históricos vinculados continuam intactos (US-010 §4,
/// mesmo espírito do cenário "Desativação de produto"). Reativação é feita por
/// <c>PATCH /v1/catalog/categories/:id</c> com <c>isActive: true</c>. Porta de
/// <c>DELETE /v1/catalog/categories/:id</c>.
/// </summary>
public sealed record DeactivateCategoryCommand(Guid CategoryId) : ICommand;
