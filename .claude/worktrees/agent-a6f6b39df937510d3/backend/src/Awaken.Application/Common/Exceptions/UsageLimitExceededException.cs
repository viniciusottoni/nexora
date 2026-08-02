namespace Awaken.Application.Common.Exceptions;

/// <summary>
/// US-230: lançada quando o usuário excede o limite de uso de um item
/// consumível para o período configurado. Mapeada para HTTP 422 no
/// ExceptionHandlingMiddleware.
/// </summary>
public class UsageLimitExceededException(string itemKey, int limit, string period)
    : Exception($"Usage limit exceeded for item '{itemKey}': {limit} per {period}.")
{
    public string ItemKey { get; } = itemKey;
    public int Limit { get; } = limit;
    public string Period { get; } = period;
}
