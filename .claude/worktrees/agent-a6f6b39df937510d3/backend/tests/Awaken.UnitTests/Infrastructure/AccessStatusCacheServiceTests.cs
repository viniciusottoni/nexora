// US-205: unit tests for AccessStatusCacheService.
using Awaken.Application.Common.Interfaces;
using Awaken.Infrastructure.Cache;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Infrastructure;

public class AccessStatusCacheServiceTests
{
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly AccessStatusCacheService _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public AccessStatusCacheServiceTests()
    {
        _sut = new AccessStatusCacheService(_cacheService.Object);
    }

    private static string Key(Guid userId) => $"access-status:{userId}";

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenCacheIsEmpty()
    {
        _cacheService
            .Setup(c => c.GetAsync<string>(Key(_userId), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var result = await _sut.GetAsync(_userId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_ReturnsValue_WhenCacheHasEntry()
    {
        const string expected = "trial_active";
        _cacheService
            .Setup(c => c.GetAsync<string>(Key(_userId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetAsync(_userId);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task SetAsync_CallsCacheServiceWithCorrectKeyAndTtl()
    {
        const string accessStatus = "subscription_active";

        await _sut.SetAsync(_userId, accessStatus);

        _cacheService.Verify(c => c.SetAsync(
            Key(_userId),
            accessStatus,
            TimeSpan.FromSeconds(60),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InvalidateAsync_RemovesKeyFromCache()
    {
        await _sut.InvalidateAsync(_userId);

        _cacheService.Verify(c => c.RemoveAsync(Key(_userId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAsync_AfterSet_ReturnsCorrectValue()
    {
        const string status = "trial_expired";
        string? stored = null;

        _cacheService
            .Setup(c => c.SetAsync(Key(_userId), status, TimeSpan.FromSeconds(60), It.IsAny<CancellationToken>()))
            .Callback<string, string, TimeSpan?, CancellationToken>((_, v, _, _) => stored = v)
            .Returns(Task.CompletedTask);

        _cacheService
            .Setup(c => c.GetAsync<string>(Key(_userId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => stored);

        await _sut.SetAsync(_userId, status);
        var result = await _sut.GetAsync(_userId);

        result.Should().Be(status);
    }

    [Fact]
    public async Task InvalidateAsync_AfterSet_GetReturnsNull()
    {
        const string status = "subscription_active";
        string? stored = status;

        _cacheService
            .Setup(c => c.RemoveAsync(Key(_userId), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((_, _) => stored = null)
            .Returns(Task.CompletedTask);

        _cacheService
            .Setup(c => c.GetAsync<string>(Key(_userId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => stored);

        await _sut.InvalidateAsync(_userId);
        var result = await _sut.GetAsync(_userId);

        result.Should().BeNull();
    }
}
