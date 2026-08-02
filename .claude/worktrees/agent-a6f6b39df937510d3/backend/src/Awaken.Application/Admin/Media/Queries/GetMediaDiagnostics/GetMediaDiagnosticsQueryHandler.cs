using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Admin.Media;
using Awaken.Domain.Repositories;
using Awaken.Shared.Admin;
using MediatR;

namespace Awaken.Application.Admin.Media.Queries.GetMediaDiagnostics;

/// <summary>
/// US-222: handler dos cards de cobertura de mídia/CDN.
///
/// Decisão pragmática de performance/custo (MVP): cobertura de imagem/vídeo/GIF (presença de URL)
/// é calculada sobre o catálogo inteiro (consulta leve, sem rede externa). Já indicadores que
/// dependem de HEAD ao vivo (links inválidos, cache/CDN, tempo médio de carregamento) são
/// calculados apenas sobre uma amostra limitada (<see cref="SampleSize"/> exercícios mais
/// recentes) para não disparar centenas de requisições HTTP síncronas a cada carregamento do
/// dashboard. O card informa quantos exercícios entraram na amostra (ExercisesCheckedSample).
/// </summary>
public class GetMediaDiagnosticsQueryHandler(
    IExerciseCatalogRepository exerciseCatalogRepository,
    IMediaDiagnosticsService mediaDiagnosticsService,
    IDateTimeService dateTimeService)
    : IRequestHandler<GetMediaDiagnosticsQuery, MediaCoverageResponse>
{
    private const int SampleSize = 50;

    public async Task<MediaCoverageResponse> Handle(
        GetMediaDiagnosticsQuery request, CancellationToken cancellationToken)
    {
        var all = (await exerciseCatalogRepository.GetAllAsync(cancellationToken)).ToList();
        var now = dateTimeService.UtcNow;

        if (all.Count == 0)
        {
            return new MediaCoverageResponse(
                DomainHealthStatus.NoData, 0, 0, 0, 0, 0, 0, null, now);
        }

        var withImage = all.Count(e => !string.IsNullOrWhiteSpace(e.ImageUrl));
        var withVideoOrGif = all.Count(e =>
            !string.IsNullOrWhiteSpace(e.VideoUrl) || !string.IsNullOrWhiteSpace(e.GifUrl));

        var percentWithImage = Math.Round(100.0 * withImage / all.Count, 1);
        var percentWithVideoOrGif = Math.Round(100.0 * withVideoOrGif / all.Count, 1);

        var sample = all
            .OrderByDescending(e => e.UpdatedAtUtc)
            .Take(SampleSize)
            .ToList();

        var invalidLinkCount = 0;
        var noCacheDetectedCount = 0;
        var latencies = new List<double>();

        foreach (var exercise in sample)
        {
            var diagnostics = await mediaDiagnosticsService.DiagnoseAsync(
                exercise.Id, exercise.ImageUrl, exercise.VideoUrl, exercise.GifUrl, cancellationToken);

            var assets = new[] { diagnostics.Image, diagnostics.Video, diagnostics.Gif };

            if (assets.Any(a => a.Status == MediaAssetStatus.Invalid))
                invalidLinkCount++;

            foreach (var asset in assets.Where(a => a.Status == MediaAssetStatus.Valid))
            {
                if (asset.LatencyMs is { } latency)
                    latencies.Add(latency);

                // CdnCacheDetected == false significa "verificamos e não há sinal de cache".
                // null significa "sem dados" (resposta não trouxe nenhum header conhecido) — RN: não inventar.
                if (asset.CdnCacheDetected == false)
                    noCacheDetectedCount++;
            }
        }

        var averageLoadTimeMs = latencies.Count > 0 ? Math.Round(latencies.Average(), 1) : (double?)null;

        var overallStatus = DetermineOverallStatus(percentWithImage, percentWithVideoOrGif, invalidLinkCount);

        return new MediaCoverageResponse(
            overallStatus,
            all.Count,
            sample.Count,
            percentWithImage,
            percentWithVideoOrGif,
            invalidLinkCount,
            noCacheDetectedCount,
            averageLoadTimeMs,
            now);
    }

    private static string DetermineOverallStatus(
        double percentWithImage, double percentWithVideoOrGif, int invalidLinkCount)
    {
        if (invalidLinkCount > 0 || percentWithImage < 50)
            return DomainHealthStatus.Critical;

        if (percentWithImage < 90 || percentWithVideoOrGif < 50)
            return DomainHealthStatus.Attention;

        return DomainHealthStatus.Healthy;
    }
}
