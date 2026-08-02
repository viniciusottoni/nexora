namespace Awaken.Application.Common.Exceptions;

public class UnauthorizedException(string code = "UNAUTHORIZED", string message = "Unauthorized.")
    : Exception(message)
{
    public string Code { get; } = code;
}
