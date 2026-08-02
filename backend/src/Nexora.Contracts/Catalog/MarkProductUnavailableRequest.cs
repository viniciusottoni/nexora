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
/// </summary>
public sealed record MarkProductUnavailableRequest(string Reason, bool AutoRestoreNextDay = true);
