using System.Net;
using Awaken.Application.Common.Interfaces;
using Awaken.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;

namespace Awaken.UnitTests.Admin.Media;

/// <summary>
/// US-222 — testes do serviço de diagnóstico de mídia via HTTP HEAD.
///
/// RN-004: garante que apenas HEAD é usado (nunca GET do binário).
/// CA: URL ausente é classificada como "sem mídia" (Missing).
/// CA: HEAD com 200 OK é classificado como Valid.
/// CA: HEAD com erro HTTP/timeout é classificado como Invalid.
/// CA: header CF-Cache-Status HIT é reconhecido como sinal de CDN ativo.
/// CA: ausência de qualquer header de cache conhecido retorna "sem dados" (null), nunca inventado.
/// </summary>
public class MediaDiagnosticsServiceTests
{
    private readonly Mock<ICacheService> _cacheService = new();

    public MediaDiagnosticsServiceTests()
    {
        // Cache sempre "miss" e SetAsync no-op, salvo quando um teste sobrescrever explicitamente.
        _cacheService
            .Setup(c => c.GetAsync<MediaAssetDiagnostic>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MediaAssetDiagnostic?)null);
    }

    private static Mock<HttpMessageHandler> CreateHandlerMock(
        HttpStatusCode statusCode, Action<HttpResponseMessage>? configureResponse = null, bool throwTimeout = false)
    {
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Head),
                ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>((_, ct) =>
            {
                if (throwTimeout)
                    throw new TaskCanceledException("Simulated timeout");

                var response = new HttpResponseMessage(statusCode)
                {
                    Content = new ByteArrayContent([]),
                };
                configureResponse?.Invoke(response);
                return Task.FromResult(response);
            });

        return handler;
    }

    private MediaDiagnosticsService CreateService(Mock<HttpMessageHandler> handlerMock)
    {
        var httpClient = new HttpClient(handlerMock.Object);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        return new MediaDiagnosticsService(
            factory.Object, _cacheService.Object, NullLogger<MediaDiagnosticsService>.Instance);
    }

    [Fact]
    public async Task DiagnoseAsync_WhenUrlIsAbsent_ReturnsMissing()
    {
        var handlerMock = CreateHandlerMock(HttpStatusCode.OK);
        var service = CreateService(handlerMock);

        var result = await service.DiagnoseAsync(Guid.NewGuid(), imageUrl: null, videoUrl: null, gifUrl: null);

        result.Image.Status.Should().Be(MediaAssetStatus.Missing);
        result.Video.Status.Should().Be(MediaAssetStatus.Missing);
        result.Gif.Status.Should().Be(MediaAssetStatus.Missing);

        handlerMock.Protected().Verify(
            "SendAsync", Times.Never(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task DiagnoseAsync_WhenHeadReturns200_ReturnsValid()
    {
        var handlerMock = CreateHandlerMock(HttpStatusCode.OK);
        var service = CreateService(handlerMock);

        var result = await service.DiagnoseAsync(
            Guid.NewGuid(), imageUrl: "https://cdn.awaken.app/exercise.jpg", videoUrl: null, gifUrl: null);

        result.Image.Status.Should().Be(MediaAssetStatus.Valid);
        result.Image.HttpStatusCode.Should().Be(200);
        result.Image.LatencyMs.Should().NotBeNull();
    }

    [Fact]
    public async Task DiagnoseAsync_OnlyUsesHeadMethod_NeverDownloadsBinary()
    {
        // RN-004: API não deve servir/baixar o arquivo pesado — apenas HEAD.
        var handlerMock = CreateHandlerMock(HttpStatusCode.OK);
        var service = CreateService(handlerMock);

        await service.DiagnoseAsync(Guid.NewGuid(), "https://cdn.awaken.app/x.jpg", null, null);

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Head),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task DiagnoseAsync_WhenHeadReturnsNotFound_ReturnsInvalid()
    {
        var handlerMock = CreateHandlerMock(HttpStatusCode.NotFound);
        var service = CreateService(handlerMock);

        var result = await service.DiagnoseAsync(
            Guid.NewGuid(), imageUrl: "https://cdn.awaken.app/missing.jpg", videoUrl: null, gifUrl: null);

        result.Image.Status.Should().Be(MediaAssetStatus.Invalid);
        result.Image.HttpStatusCode.Should().Be(404);
    }

    [Fact]
    public async Task DiagnoseAsync_WhenRequestTimesOut_ReturnsInvalid()
    {
        var handlerMock = CreateHandlerMock(HttpStatusCode.OK, throwTimeout: true);
        var service = CreateService(handlerMock);

        var result = await service.DiagnoseAsync(
            Guid.NewGuid(), imageUrl: "https://cdn.awaken.app/slow.jpg", videoUrl: null, gifUrl: null);

        result.Image.Status.Should().Be(MediaAssetStatus.Invalid);
        result.Image.HttpStatusCode.Should().BeNull();
    }

    [Fact]
    public async Task DiagnoseAsync_WhenCfCacheStatusHit_DetectsCdnActive()
    {
        var handlerMock = CreateHandlerMock(HttpStatusCode.OK,
            response => response.Headers.TryAddWithoutValidation("CF-Cache-Status", "HIT"));
        var service = CreateService(handlerMock);

        var result = await service.DiagnoseAsync(
            Guid.NewGuid(), imageUrl: "https://cdn.awaken.app/cached.jpg", videoUrl: null, gifUrl: null);

        result.Image.CdnCacheDetected.Should().BeTrue();
    }

    [Fact]
    public async Task DiagnoseAsync_WhenNoCacheHeaderPresent_ReturnsNullNotInvented()
    {
        var handlerMock = CreateHandlerMock(HttpStatusCode.OK);
        var service = CreateService(handlerMock);

        var result = await service.DiagnoseAsync(
            Guid.NewGuid(), imageUrl: "https://cdn.awaken.app/no-cache-header.jpg", videoUrl: null, gifUrl: null);

        result.Image.CdnCacheDetected.Should().BeNull("sem header de cache conhecido, não deve inventar dado");
    }
}
