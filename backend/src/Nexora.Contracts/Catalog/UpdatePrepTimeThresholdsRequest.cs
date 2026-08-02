namespace Nexora.Contracts.Catalog;

/// <summary>
/// US-016 — corpo de <c>PATCH /v1/catalog/variants/{id}/prep-time</c>. <see cref="WarnMinutes"/>
/// e <see cref="CriticalMinutes"/> nulos significam "herdar o padrão do tenant" (ver
/// <see cref="PrepTimeAnalysisResponse"/> para os valores efetivos já resolvidos).
/// </summary>
public sealed record UpdatePrepTimeThresholdsRequest(short PrepMinutes, short? WarnMinutes, short? CriticalMinutes);
