namespace Nexora.Application.Abstractions.Messaging;

/// <summary>
/// Padrão de retorno de handlers — nunca lançar exceção para erro de negócio (ver ADR-021).
/// Espelha o Result/Result&lt;T&gt; do seminarioteologico.
/// </summary>
public class Result
{
    protected Result(
        bool isSuccess,
        string? error,
        string? code,
        IReadOnlyDictionary<string, string[]>? errors)
    {
        IsSuccess = isSuccess;
        Error = error;
        Code = code;
        Errors = errors;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public string? Error { get; }

    /// <summary>Código estável de <c>Nexora.Shared.Errors.ApiErrorCodes</c> (ADR-021).</summary>
    public string? Code { get; }

    public IReadOnlyDictionary<string, string[]>? Errors { get; }

    public static Result Success() => new(true, error: null, code: null, errors: null);

    public static Result Failure(
        string error,
        string? code = null,
        IReadOnlyDictionary<string, string[]>? errors = null) =>
        new(false, error, code, errors);
}

public sealed class Result<T> : Result
{
    private Result(
        bool isSuccess,
        T? value,
        string? error,
        string? code,
        IReadOnlyDictionary<string, string[]>? errors)
        : base(isSuccess, error, code, errors)
    {
        Value = value;
    }

    public T? Value { get; }

    public static Result<T> Success(T value) =>
        new(true, value, error: null, code: null, errors: null);

    public new static Result<T> Failure(
        string error,
        string? code = null,
        IReadOnlyDictionary<string, string[]>? errors = null) =>
        new(false, default, error, code, errors);
}
