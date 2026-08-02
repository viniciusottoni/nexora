namespace Awaken.Contracts.Admin.Media;

/// <summary>
/// US-222: status de mídia de um exercício do catálogo.
/// RN-005: expõe apenas a URL pública já cadastrada no catálogo (a mesma usada pelo app) — nunca
/// chave/segredo de storage. As URLs aqui já são públicas por design (servidas via storage/CDN,
/// não pela API — RN-004 desta US e US-214).
/// </summary>
public record ExerciseMediaSummaryResponse(
    Guid ExerciseId,
    string NamePtBr,
    string Slug,
    string Environment,
    string DifficultyLevel,
    string EquipmentCategory,
    string PrimaryRegion,
    string MediaStatus,
    string? ImageUrl,
    string? ImageStatus,
    double? ImageLatencyMs,
    string? VideoUrl,
    string? VideoStatus,
    double? VideoLatencyMs,
    string? GifUrl,
    string? GifStatus,
    double? GifLatencyMs,
    bool? CdnCacheDetected);
