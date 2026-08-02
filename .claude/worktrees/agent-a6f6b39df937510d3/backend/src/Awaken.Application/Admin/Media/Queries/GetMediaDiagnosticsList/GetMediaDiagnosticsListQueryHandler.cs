using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Admin.Media;
using Awaken.Domain.Entities.Exercises;
using Awaken.Domain.Repositories;
using Awaken.Shared.Admin;
using MediatR;

namespace Awaken.Application.Admin.Media.Queries.GetMediaDiagnosticsList;

/// <summary>
/// US-222: handler de listagem/filtro de exercícios com problema de mídia para o admin site.
///
/// Decisão pragmática de performance/custo (MVP): os filtros estruturais (ambiente, dificuldade,
/// equipamento, região muscular) são aplicados primeiro sobre o catálogo inteiro (consulta em
/// memória, sem rede externa). Só então o diagnóstico de mídia "ao vivo" (HEAD HTTP, via
/// <see cref="IMediaDiagnosticsService"/>, que já cacheia por TTL curto) roda sobre um teto de
/// <see cref="MaxLiveCheck"/> exercícios filtrados — paginação e filtro por status de mídia são
/// aplicados depois, sobre esse subconjunto já diagnosticado. Isso evita HEAD síncrono contra
/// todo o catálogo a cada request.
/// </summary>
public class GetMediaDiagnosticsListQueryHandler(
    IExerciseCatalogRepository exerciseCatalogRepository,
    IMediaDiagnosticsService mediaDiagnosticsService)
    : IRequestHandler<GetMediaDiagnosticsListQuery, MediaDiagnosticsListResponse>
{
    private const int MaxLiveCheck = 200;

    /// <summary>Acima deste limiar de latência (ms), um asset válido é considerado "lento" (RN-003).</summary>
    private const double SlowAssetThresholdMs = 1500;

    public async Task<MediaDiagnosticsListResponse> Handle(
        GetMediaDiagnosticsListQuery request, CancellationToken cancellationToken)
    {
        var all = await exerciseCatalogRepository.GetAllAsync(cancellationToken);

        var structurallyFiltered = all
            .Where(e => MatchesFilter(request.Environment, e.Environment))
            .Where(e => MatchesFilter(request.DifficultyLevel, e.DifficultyLevel))
            .Where(e => MatchesFilter(request.EquipmentCategory, e.EquipmentCategory))
            .Where(e => MatchesFilter(request.PrimaryRegion, e.PrimaryRegion))
            .OrderByDescending(e => e.UpdatedAtUtc)
            .Take(MaxLiveCheck)
            .ToList();

        var diagnosed = new List<ExerciseMediaSummaryResponse>();

        foreach (var exercise in structurallyFiltered)
        {
            var summary = await BuildSummaryAsync(exercise, cancellationToken);

            if (request.MediaStatus is null || summary.MediaStatus == request.MediaStatus)
                diagnosed.Add(summary);
        }

        var total = diagnosed.Count;
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Max(1, request.PageSize);

        var pageItems = diagnosed
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new MediaDiagnosticsListResponse(pageItems, total, page, pageSize);
    }

    private async Task<ExerciseMediaSummaryResponse> BuildSummaryAsync(
        ExerciseCatalog exercise, CancellationToken cancellationToken)
    {
        var diagnostics = await mediaDiagnosticsService.DiagnoseAsync(
            exercise.Id, exercise.ImageUrl, exercise.VideoUrl, exercise.GifUrl, cancellationToken);

        var mediaStatus = ClassifyMediaStatus(diagnostics);
        var cdnCacheDetected = AggregateCdnSignal(diagnostics);

        return new ExerciseMediaSummaryResponse(
            exercise.Id,
            exercise.NamePtBr,
            exercise.Slug,
            exercise.Environment,
            exercise.DifficultyLevel,
            exercise.EquipmentCategory,
            exercise.PrimaryRegion,
            mediaStatus,
            exercise.ImageUrl,
            ToStatusLabel(diagnostics.Image.Status),
            diagnostics.Image.LatencyMs,
            exercise.VideoUrl,
            ToStatusLabel(diagnostics.Video.Status),
            diagnostics.Video.LatencyMs,
            exercise.GifUrl,
            ToStatusLabel(diagnostics.Gif.Status),
            diagnostics.Gif.LatencyMs,
            cdnCacheDetected);
    }

    private string ClassifyMediaStatus(MediaAssetDiagnostics diagnostics)
    {
        var assets = new[] { diagnostics.Image, diagnostics.Video, diagnostics.Gif };

        // RN-002: link inválido é problema operacional — prioridade sobre os demais status.
        if (assets.Any(a => a.Status == MediaAssetStatus.Invalid))
            return MediaAssetStatusLabels.InvalidLink;

        var validAssets = assets.Where(a => a.Status == MediaAssetStatus.Valid).ToList();

        // RN-001: exercício sem nenhuma mídia mínima (imagem, GIF ou vídeo).
        if (validAssets.Count == 0)
            return MediaAssetStatusLabels.Missing;

        // RN-003: asset pesado/lento aparece como atenção.
        if (validAssets.Any(a => a.LatencyMs is { } latency && latency > SlowAssetThresholdMs))
            return MediaAssetStatusLabels.Slow;

        return MediaAssetStatusLabels.Ok;
    }

    private static bool? AggregateCdnSignal(MediaAssetDiagnostics diagnostics)
    {
        var validAssets = new[] { diagnostics.Image, diagnostics.Video, diagnostics.Gif }
            .Where(a => a.Status == MediaAssetStatus.Valid)
            .ToList();

        if (validAssets.Count == 0)
            return null;

        if (validAssets.Any(a => a.CdnCacheDetected == true))
            return true;

        if (validAssets.All(a => a.CdnCacheDetected == false))
            return false;

        // Algum asset válido sem header de cache detectável — "sem dados", não inventar.
        return null;
    }

    private static string? ToStatusLabel(MediaAssetStatus status) => status switch
    {
        MediaAssetStatus.Missing => "missing",
        MediaAssetStatus.Valid => "valid",
        MediaAssetStatus.Invalid => "invalid",
        _ => null,
    };

    private static bool MatchesFilter(string? filterValue, string fieldValue) =>
        string.IsNullOrWhiteSpace(filterValue)
        || string.Equals(filterValue, fieldValue, StringComparison.OrdinalIgnoreCase);
}
