using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Cashier;

namespace Nexora.Application.Cashier.Commands.ApplyDiscount;

/// <summary>
/// Porta de <c>POST /v1/sessions/{id}/discount</c> (US-054, RN-011) — desconto sobre o total da
/// sessão (<see cref="Scope"/> <c>SESSION</c>) ou sobre um item específico (<c>ITEM</c>, requer
/// <see cref="OrderItemId"/>). Acima do limite configurado
/// (<c>Nexora.Application.Cashier.Support.DiscountPolicy</c>), exige <see cref="AuthorizationToken"/>
/// válido para a ação <c>DISCOUNT_ABOVE_LIMIT</c> (ADR-023, já catalogada em <c>SensitiveActionCatalog</c>).
/// </summary>
public sealed record ApplyDiscountCommand(
    Guid SessionId,
    decimal? Percent,
    decimal? Amount,
    string Reason,
    string Scope,
    Guid? OrderItemId,
    string? AuthorizationToken) : ICommand<ApplyDiscountResponse>;
