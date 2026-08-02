namespace Awaken.Application.Common.Exceptions;

public class ValidationException(IEnumerable<FluentValidation.Results.ValidationFailure> failures)
    : Exception("One or more validation failures occurred.")
{
    public IDictionary<string, string[]> Errors { get; } = failures
        .GroupBy(f => f.PropertyName, f => f.ErrorMessage)
        .ToDictionary(g => g.Key, g => g.ToArray());
}
