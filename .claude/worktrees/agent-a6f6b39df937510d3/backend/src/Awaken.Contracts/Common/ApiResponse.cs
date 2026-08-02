namespace Awaken.Contracts.Common;

public record ApiResponse<T>(T Data, string CorrelationId);

public record ApiErrorResponse(
    string Message,
    string Code,
    IEnumerable<FieldError>? Errors,
    string CorrelationId);

public record FieldError(string Field, string Message);

/// US-193: wrapper de paginação genérico para endpoints admin read-only.
public record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    bool HasMore);
