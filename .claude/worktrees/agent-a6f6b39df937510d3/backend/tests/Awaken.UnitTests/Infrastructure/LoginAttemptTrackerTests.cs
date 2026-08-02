using Awaken.Infrastructure.Services;
using FluentAssertions;
using Moq;
using StackExchange.Redis;

namespace Awaken.UnitTests.Infrastructure;

public class LoginAttemptTrackerTests
{
    private readonly Mock<IConnectionMultiplexer> _multiplexer = new();
    private readonly Mock<IDatabase> _db = new();

    public LoginAttemptTrackerTests()
    {
        _multiplexer.SetupGet(m => m.IsConnected).Returns(true);
        _multiplexer.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_db.Object);
    }

    private LoginAttemptTracker CreateTracker() => new(_multiplexer.Object);

    [Fact]
    public async Task IsLockedOutAsync_ReturnsFalse_WhenLockoutKeyDoesNotExist()
    {
        _db.Setup(d => d.KeyExistsAsync(It.Is<RedisKey>(k => k.ToString().StartsWith("login-lockout:")), It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);

        var result = await CreateTracker().IsLockedOutAsync("hunter@awaken.app");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsLockedOutAsync_ReturnsTrue_WhenLockoutKeyExists()
    {
        _db.Setup(d => d.KeyExistsAsync(It.Is<RedisKey>(k => k.ToString().StartsWith("login-lockout:")), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        var result = await CreateTracker().IsLockedOutAsync("Hunter@Awaken.APP");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsLockedOutAsync_NormalizesEmail_BeforeCheckingKey()
    {
        RedisKey capturedKey = default;
        _db.Setup(d => d.KeyExistsAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .Callback<RedisKey, CommandFlags>((key, _) => capturedKey = key)
            .ReturnsAsync(false);

        await CreateTracker().IsLockedOutAsync("  HUNTER@AWAKEN.APP  ");

        capturedKey.ToString().Should().Be("login-lockout:hunter@awaken.app");
    }

    [Fact]
    public async Task RecordFailureAsync_IncrementsCounter_AndSetsExpiry_OnFirstFailure()
    {
        _db.Setup(d => d.StringIncrementAsync(It.IsAny<RedisKey>(), It.IsAny<long>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(1);
        _db.Setup(d => d.KeyExpireAsync(It.IsAny<RedisKey>(), It.IsAny<TimeSpan?>(), It.IsAny<ExpireWhen>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        await CreateTracker().RecordFailureAsync("hunter@awaken.app");

        _db.Verify(d => d.KeyExpireAsync(
            It.Is<RedisKey>(k => k.ToString().StartsWith("login-attempts:")),
            It.Is<TimeSpan?>(t => t == TimeSpan.FromMinutes(10)),
            It.IsAny<ExpireWhen>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task RecordFailureAsync_DoesNotSetExpiry_OnSubsequentFailures()
    {
        _db.Setup(d => d.StringIncrementAsync(It.IsAny<RedisKey>(), It.IsAny<long>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(3);

        await CreateTracker().RecordFailureAsync("hunter@awaken.app");

        _db.Verify(d => d.KeyExpireAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<ExpireWhen>(),
            It.IsAny<CommandFlags>()), Times.Never);
    }

    [Fact]
    public async Task RecordFailureAsync_SetsLockout_AndClearsCounter_WhenMaxAttemptsReached()
    {
        _db.Setup(d => d.StringIncrementAsync(It.IsAny<RedisKey>(), It.IsAny<long>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(5);
        _db.Setup(d => d.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);
        _db.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        await CreateTracker().RecordFailureAsync("hunter@awaken.app");

        _db.Verify(d => d.KeyDeleteAsync(
            It.Is<RedisKey>(k => k.ToString().StartsWith("login-attempts:")),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task ClearAsync_DeletesBothCounterAndLockoutKeys()
    {
        _db.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        await CreateTracker().ClearAsync("hunter@awaken.app");

        _db.Verify(d => d.KeyDeleteAsync(
            It.Is<RedisKey>(k => k.ToString() == "login-attempts:hunter@awaken.app"),
            It.IsAny<CommandFlags>()), Times.Once);

        _db.Verify(d => d.KeyDeleteAsync(
            It.Is<RedisKey>(k => k.ToString() == "login-lockout:hunter@awaken.app"),
            It.IsAny<CommandFlags>()), Times.Once);
    }
}
