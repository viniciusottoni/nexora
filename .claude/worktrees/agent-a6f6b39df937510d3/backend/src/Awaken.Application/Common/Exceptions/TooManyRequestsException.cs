namespace Awaken.Application.Common.Exceptions;

public class TooManyRequestsException(string code = "TOO_MANY_REQUESTS", string message = "Muitas tentativas. Aguarde e tente novamente.")
    : Exception(message)
{
    public string Code { get; } = code;
}
