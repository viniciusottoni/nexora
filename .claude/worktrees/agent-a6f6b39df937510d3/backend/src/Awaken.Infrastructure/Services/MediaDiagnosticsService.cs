using System.Security.Cryptography;
using System.Text;
using Awaken.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Awaken.Infrastructure.Services;

/// <summary>
/// US-222: diagnostica disponibilidade de mídia (imagem/GIF/vídeo) do catálogo de exercícios via HTTP HEAD.
///
/// RN-004: API não serve o arquivo pesado diretamente — apenas HEAD, nunca GET do binário.
/// RN-005: não lê nem expõe credenciais de storage (R2); opera somente sobre a URL pública já
/// persistida em ExerciseCatalog.
///
/// Decisão pragmática de performance/custo (MVP): o resultado de cada asset é cacheado em
/// ICacheService (Redis) por TTL curto (10 minutos), chaveado por exercício+URL. Isso evita que
/// cada carregamento do dashboard admin dispare HEAD contra o storage/CDN para o catálogo inteiro;
/// o handler de lista (GetMediaDiagnosticsList) também limita quantos exercícios são checados
/// "ao vivo" por página, então o custo cresce com uso real do admin, não com o tamanho do catálogo.
/// </summary>
public class MediaDiagnosticsService(
    IHttpClientFactory httpClientFactory,
    ICacheService cacheService,
    ILogger<MediaDiagnosticsService> logger) : IMediaDiagnosticsService
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(4);
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);
    private const string CacheKeyPrefix = "admin:media-diagnostics:";

    public async Task<MediaAssetDiagnostics> DiagnoseAsync(
        Guid exerciseId,
        string? imageUrl,
        string? videoUrl,
        string? gifUrl,
        CancellationToken cancellationToken = default)
    {
        var image = await DiagnoseAssetAsync(exerciseId, "image", imageUrl, cancellationToken);
        var video = await DiagnoseAssetAsync(exerciseId, "video", videoUrl, cancellationToken);
        var gif = await DiagnoseAssetAsync(exerciseId, "gif", gifUrl, cancellationToken);

        return new MediaAssetDiagnostics(exerciseId, image, video, gif);
    }

    private async Task<MediaAssetDiagnostic> DiagnoseAssetAsync(
        Guid exerciseId, string assetKind, string? url, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(url))
            return new MediaAssetDiagnostic(MediaAssetStatus.Missing, null, null, null);

        var cacheKey = BuildCacheKey(exerciseId, assetKind, url);

        try
        {
            var cached = await cacheService.GetAsync<MediaAssetDiagnostic>(cacheKey, cancellationToken);
            if (cached is not null)
                return cached;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Redis indisponível para cache de diagnóstico de mídia (exercício {ExerciseId}, asset {AssetKind}); seguindo sem cache",
                exerciseId, assetKind);
        }

        var diagnostic = await ProbeAsync(url, cancellationToken);

        try
        {
            await cacheService.SetAsync(cacheKey, diagnostic, CacheTtl, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Redis indisponível ao gravar cache de diagnóstico de mídia (exercício {ExerciseId}, asset {AssetKind})",
                exerciseId, assetKind);
        }

        return diagnostic;
    }

    private async Task<MediaAssetDiagnostic> ProbeAsync(string url, CancellationToken cancellationToken)
    {
        // Clientes do IHttpClientFactory são gerenciados pelo pool interno (HttpMessageHandler
        // reaproveitado) e não devem ser descartados manualmente a cada chamada.
        var client = httpClientFactory.CreateClient(nameof(MediaDiagnosticsService));
        client.Timeout = RequestTimeout;

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(RequestTimeout);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            using var response = await client.SendAsync(request, timeoutCts.Token);
            stopwatch.Stop();

            var latencyMs = stopwatch.Elapsed.TotalMilliseconds;

            if (!response.IsSuccessStatusCode)
            {
                return new MediaAssetDiagnostic(
                    MediaAssetStatus.Invalid, (int)response.StatusCode, latencyMs, null);
            }

            // Sinal de CDN/cache ativo: só reportamos quando a resposta HEAD efetivamente traz um
            // cabeçalho de cache conhecido (CF-Cache-Status do Cloudflare, ou Age/X-Cache genéricos).
            // Quando nenhum desses headers existe, marcamos null ("sem dados") em vez de inferir.
            var cdnDetected = DetectCdnCache(response.Headers, response.Content.Headers);

            return new MediaAssetDiagnostic(MediaAssetStatus.Valid, (int)response.StatusCode, latencyMs, cdnDetected);
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            return new MediaAssetDiagnostic(MediaAssetStatus.Invalid, null, stopwatch.Elapsed.TotalMilliseconds, null);
        }
        catch (HttpRequestException)
        {
            stopwatch.Stop();
            return new MediaAssetDiagnostic(MediaAssetStatus.Invalid, null, stopwatch.Elapsed.TotalMilliseconds, null);
        }
    }

    private static bool? DetectCdnCache(
        System.Net.Http.Headers.HttpResponseHeaders headers,
        System.Net.Http.Headers.HttpContentHeaders contentHeaders)
    {
        if (headers.TryGetValues("CF-Cache-Status", out var cfStatus))
        {
            var value = cfStatus.FirstOrDefault();
            return value is not null &&
                   (value.Equals("HIT", StringComparison.OrdinalIgnoreCase)
                    || value.Equals("STALE", StringComparison.OrdinalIgnoreCase)
                    || value.Equals("REVALIDATED", StringComparison.OrdinalIgnoreCase));
        }

        if (headers.TryGetValues("X-Cache", out var xCache))
        {
            var value = xCache.FirstOrDefault();
            return value is not null && value.Contains("HIT", StringComparison.OrdinalIgnoreCase);
        }

        if (headers.Age is not null)
            return headers.Age.Value.TotalSeconds > 0;

        // Nenhum header de cache/CDN conhecido presente na resposta — "sem dados", não inventar.
        return null;
    }

    private static string BuildCacheKey(Guid exerciseId, string assetKind, string url)
    {
        var urlHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(url)))[..16];
        return $"{CacheKeyPrefix}{exerciseId}:{assetKind}:{urlHash}";
    }
}
