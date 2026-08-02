namespace Awaken.Application.Common.Interfaces;

/// <summary>
/// US-222: diagnóstico de mídia/CDN de um exercício do catálogo.
///
/// RN-004: nunca baixa o binário do asset — apenas HTTP HEAD, para não transformar a API
/// admin em proxy de arquivo pesado.
/// RN-005: não expõe nenhum dado de acesso ao storage (chaves/segredos R2); apenas classifica
/// a URL pública já presente no catálogo.
/// </summary>
public interface IMediaDiagnosticsService
{
    /// <summary>
    /// Executa HEAD nas URLs de mídia (imagem, GIF, vídeo) de um exercício e classifica o status
    /// de cada asset. Resultado é cacheado por um TTL curto (ver implementação) para evitar HEAD
    /// repetido a cada carregamento do dashboard.
    /// </summary>
    Task<MediaAssetDiagnostics> DiagnoseAsync(
        Guid exerciseId,
        string? imageUrl,
        string? videoUrl,
        string? gifUrl,
        CancellationToken cancellationToken = default);
}

/// <summary>Classificação de um único asset (imagem, GIF ou vídeo) de um exercício.</summary>
public enum MediaAssetStatus
{
    /// <summary>Nenhuma URL informada no catálogo para este tipo de asset.</summary>
    Missing,

    /// <summary>URL presente e HEAD retornou 200 OK dentro do timeout.</summary>
    Valid,

    /// <summary>URL presente, mas HEAD retornou erro HTTP ou timeout/exceção de rede.</summary>
    Invalid,
}

/// <summary>Resultado do diagnóstico de um asset individual.</summary>
public record MediaAssetDiagnostic(
    MediaAssetStatus Status,
    int? HttpStatusCode,
    double? LatencyMs,
    bool? CdnCacheDetected);

/// <summary>Resultado agregado do diagnóstico de mídia de um exercício.</summary>
public record MediaAssetDiagnostics(
    Guid ExerciseId,
    MediaAssetDiagnostic Image,
    MediaAssetDiagnostic Video,
    MediaAssetDiagnostic Gif);
