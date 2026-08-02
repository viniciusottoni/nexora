using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Catalog;

namespace Nexora.Application.Catalog.Availability.Commands.MarkProductUnavailable;

/// <summary>
/// Marca um produto como indisponível, com motivo (US-015 §3.1/§7 — RF-CAT-07). Porta de
/// <c>POST /v1/catalog/products/:id/availability</c> (nuvem, <see cref="SetProductAvailabilityRequest"/>
/// com <c>isAvailable=false</c>) e de <c>POST /v1/kds/products/:id/unavailable</c> (edge/KDS,
/// <see cref="MarkProductUnavailableRequest"/>) — o mesmo comando serve os dois processos, só o
/// controller/rota muda (mesmo espírito de <c>ActivateProductCommand</c>/<c>DeactivateProductCommand</c>
/// reaproveitados por um único handler independente de qual Api despacha).
///
/// <see cref="AutoRestoreNextDay"/> é persistido em <c>product.auto_restore</c>. O worker da virada
/// do dia operacional restaura somente produtos que mantiveram essa opção ativa.
/// </summary>
public sealed record MarkProductUnavailableCommand(Guid ProductId, string Reason, bool AutoRestoreNextDay = true)
    : ICommand<ProductAvailabilityResponse>;
