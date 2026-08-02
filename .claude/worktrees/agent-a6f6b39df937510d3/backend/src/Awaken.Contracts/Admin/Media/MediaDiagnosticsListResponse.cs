namespace Awaken.Contracts.Admin.Media;

/// <summary>US-222: lista paginada de exercícios e o status de mídia de cada um.</summary>
public record MediaDiagnosticsListResponse(
    IReadOnlyList<ExerciseMediaSummaryResponse> Items,
    int Total,
    int Page,
    int PageSize);
