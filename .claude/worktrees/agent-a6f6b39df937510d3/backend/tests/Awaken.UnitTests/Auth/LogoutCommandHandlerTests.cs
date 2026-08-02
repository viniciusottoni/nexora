using Awaken.Application.Auth.Commands.Logout;
using Awaken.Application.Common.Interfaces;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Auth;

public class LogoutCommandHandlerTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private LogoutCommandHandler CreateHandler() => new(
        _refreshTokenRepository.Object,
        _currentUserService.Object,
        _unitOfWork.Object);

    [Fact]
    public async Task HandleRevokesAllTokensForCurrentUserAndSaves()
    {
        var userId = Guid.NewGuid();
        _currentUserService.Setup(s => s.UserId).Returns(userId);

        var result = await CreateHandler().Handle(new LogoutCommand(), CancellationToken.None);

        result.Should().Be(MediatR.Unit.Value);
        _refreshTokenRepository.Verify(
            r => r.RevokeAllByUserIdAsync(userId, It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleSavesAfterRevocation()
    {
        var callOrder = new List<string>();
        var userId = Guid.NewGuid();
        _currentUserService.Setup(s => s.UserId).Returns(userId);
        _refreshTokenRepository
            .Setup(r => r.RevokeAllByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("revoke"))
            .Returns(Task.CompletedTask);
        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("save"))
            .ReturnsAsync(1);

        await CreateHandler().Handle(new LogoutCommand(), CancellationToken.None);

        callOrder.Should().ContainInOrder("revoke", "save");
    }
}
