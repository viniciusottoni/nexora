namespace Awaken.Application.Common.Exceptions;

/// <summary>
/// US-227 / RN-005: lançada quando um conflito de concorrência otimista
/// (DbUpdateConcurrencyException) persiste após esgotar as tentativas de
/// retry controlado. Mapeada para HTTP 409 no ExceptionHandlingMiddleware.
/// </summary>
public class ConcurrencyConflictException(
    string code = "CONCURRENCY_CONFLICT",
    string message = "Não foi possível concluir a operação devido a uma alteração concorrente. Tente novamente.")
    : Exception(message)
{
    public string Code { get; } = code;
}
