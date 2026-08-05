namespace Nexora.Contracts.Catalog;

/// <summary>
/// Corpo de <c>POST /v1/catalog/products/:id/availability</c> (nuvem, US-015 §7 — mesmo espírito
/// de <c>POST /v1/catalog/variants/:id/availability { "isAvailable": true }</c> do doc da história,
/// adaptado para produto: ver nota de desvio no relatório da tarefa sobre granularidade
/// produto-vs-variante). Um único endpoint cobre as duas direções: <see cref="IsAvailable"/>
/// <c>false</c> exige <see cref="Reason"/> (validado por
/// <c>MarkProductUnavailableCommandValidator</c>); <c>true</c> ignora <see cref="Reason"/> e
/// <see cref="AutoRestoreNextDay"/> (retorno manual à disponibilidade).
/// </summary>
public sealed record SetProductAvailabilityRequest(bool IsAvailable, string? Reason, bool AutoRestoreNextDay = true);

/// <summary>
/// Corpo de <c>POST /v1/kds/products/:id/unavailable</c> (edge/KDS, US-015 §7/§10 — "a marcação
/// precisa caber em um toque"). Só marca indisponível; o retorno manual à disponibilidade no KDS é
/// <c>POST /v1/kds/products/:id/available</c>, sem corpo (mesmo padrão de
/// <c>ActivateProductCommand</c>).
///
/// <para>
/// <see cref="Reason"/> (US-044 §10) precisa ser um dos três valores de
/// <see cref="ProductUnavailableReasons.All"/> — validado por
/// <c>MarkProductUnavailableCommandValidator</c>, não aqui (contrato não referencia FluentValidation).
/// </para>
/// <para>
/// <see cref="OrderItemId"/> (US-044 §6, EVT-012 <c>order.item.unavailable_flagged</c>) é
/// preenchido só quando a marcação parte de um item específico já na fila do KDS (gatilho pelo
/// cartão) — distingue essa origem de uma marcação feita pela lista avulsa de "produtos
/// indisponíveis" (US-015), que não referencia nenhum item de pedido em particular. Nulo é o caso
/// comum; quando presente, dispara o evento adicional além do EVT-051 sempre emitido.
/// </para>
/// </summary>
public sealed record MarkProductUnavailableRequest(string Reason, bool AutoRestoreNextDay = true, Guid? OrderItemId = null);
