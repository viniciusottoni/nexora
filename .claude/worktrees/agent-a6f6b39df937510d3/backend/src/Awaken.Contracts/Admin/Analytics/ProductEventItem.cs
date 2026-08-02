namespace Awaken.Contracts.Admin.Analytics;

/// <summary>
/// US-168 — item de volume de evento de produto.
/// RN-004: eventos sem volume recente ainda aparecem (Volume=0, HasNoRecentVolume=true)
/// em vez de serem silenciosamente omitidos.
/// </summary>
public record ProductEventItem(string EventName, int Volume, bool HasNoRecentVolume);
