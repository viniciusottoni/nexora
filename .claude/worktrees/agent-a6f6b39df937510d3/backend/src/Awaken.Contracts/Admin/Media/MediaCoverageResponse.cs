namespace Awaken.Contracts.Admin.Media;

/// <summary>
/// US-222: cards de cobertura de mídia/CDN do catálogo de exercícios.
/// RN-005: nenhum campo aqui carrega dado de acesso ao storage (URLs, chaves, etc.) — apenas
/// indicadores agregados.
/// </summary>
public record MediaCoverageResponse(
    string OverallStatus,
    int TotalExercises,
    int ExercisesCheckedSample,
    double PercentWithImage,
    double PercentWithVideoOrGif,
    int InvalidLinkCount,
    int NoCacheDetectedCount,
    double? AverageLoadTimeMs,
    DateTime GeneratedAtUtc);
