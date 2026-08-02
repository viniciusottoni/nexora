using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Catalog;

namespace Nexora.Application.Catalog.Availability.Commands.MarkProductAvailable;

/// <summary>
/// Retorna manualmente um produto à disponibilidade (US-015 §3.1, cenário "Retorno à
/// disponibilidade, manual ou automático"). Porta de <c>POST /v1/catalog/products/:id/availability</c>
/// (nuvem, <c>isAvailable=true</c>) e de <c>POST /v1/kds/products/:id/available</c> (edge/KDS).
/// O retorno AUTOMÁTICO no início do próximo dia operacional é
/// <see cref="Nexora.Application.Catalog.Availability.Commands.RestoreProductsPastBusinessDay.RestoreProductsPastBusinessDayCommand"/>,
/// não este comando (esse é só o botão manual do gestor/cozinha).
/// </summary>
public sealed record MarkProductAvailableCommand(Guid ProductId) : ICommand<ProductAvailabilityResponse>;
