using Nexora.Application.Abstractions.Messaging;
using Nexora.Shared.Errors;
using FluentValidation;
using MediatR;

namespace Nexora.Application.Abstractions.Behaviors;

/// <summary>
/// Primeiro behavior do pipeline (ADR-037). Roda todos os validators registrados para o
/// request e converte falha em <c>Result.Failure</c>/<c>Result&lt;T&gt;.Failure</c> — nunca lança.
/// Só se aplica quando TResponse é Result ou Result&lt;T&gt; (ICommand/ICommand&lt;T&gt;/IQuery&lt;T&gt;).
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var failures = new List<FluentValidation.Results.ValidationFailure>();
        foreach (var validator in _validators)
        {
            var result = await validator.ValidateAsync(request, cancellationToken);
            failures.AddRange(result.Errors);
        }

        if (failures.Count == 0)
        {
            return await next();
        }

        var errors = failures
            .GroupBy(f => f.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).ToArray());

        const string message = "Revise os campos indicados e tente novamente.";

        return BuildFailure(message, ApiErrorCodes.ValidationError, errors);
    }

    /// <summary>
    /// TResponse é sempre Result ou Result&lt;T&gt; por construção de ICommand/IQuery — o cast
    /// é seguro; Nexora.ArchitectureTests garante que nenhum outro IRequest passa por aqui.
    /// </summary>
    private static TResponse BuildFailure(string message, string code, IReadOnlyDictionary<string, string[]> errors)
    {
        var responseType = typeof(TResponse);

        if (responseType == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(message, code, errors);
        }

        var valueType = responseType.GetGenericArguments()[0];
        var failureMethod = typeof(Result<>).MakeGenericType(valueType)
            .GetMethod(nameof(Result<object>.Failure), new[] { typeof(string), typeof(string), typeof(IReadOnlyDictionary<string, string[]>) })!;

        return (TResponse)failureMethod.Invoke(null, new object?[] { message, code, errors })!;
    }
}
