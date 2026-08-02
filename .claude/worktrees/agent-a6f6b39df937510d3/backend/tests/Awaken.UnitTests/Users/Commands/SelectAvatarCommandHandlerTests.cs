using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Application.Users.Commands.SelectAvatar;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Entities.Inventory;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Users.Commands;

public class SelectAvatarCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IDateTimeService> _dateTimeService = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IInventoryRepository> _inventoryRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime UtcNow = new(2026, 7, 2, 10, 0, 0, DateTimeKind.Utc);

    public SelectAvatarCommandHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(UserId);
        _dateTimeService.Setup(s => s.UtcNow).Returns(UtcNow);
    }

    private SelectAvatarCommandHandler CreateHandler() => new(
        _currentUserService.Object,
        _dateTimeService.Object,
        _userRepository.Object,
        _inventoryRepository.Object,
        _unitOfWork.Object);

    private User SetupExistingUser()
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        _userRepository
            .Setup(r => r.GetByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        return user;
    }

    [Fact]
    public async Task Succeeds_WhenAvatarHasNoRequiredItem_PersistsAndSaves()
    {
        var user = SetupExistingUser();

        await CreateHandler().Handle(new SelectAvatarCommand("avatar_male_1"), CancellationToken.None);

        user.SelectedAvatarKey.Should().Be("avatar_male_1");
        user.UpdatedAtUtc.Should().Be(UtcNow);
        _userRepository.Verify(r => r.Update(user), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ThrowsNotFound_WhenAvatarKeyDoesNotExistInCatalog()
    {
        var act = () => CreateHandler().Handle(
            new SelectAvatarCommand("unknown_avatar"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();

        _userRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ThrowsNotFound_WhenAvatarKeyIsArbitraryUrl_SimulatingExternalUploadAttempt()
    {
        var act = () => CreateHandler().Handle(
            new SelectAvatarCommand("https://evil.example.com/avatar.png"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ThrowsConflict_WhenAvatarRequiresPackUserDoesNotOwn()
    {
        _inventoryRepository
            .Setup(r => r.GetByUserIdAndItemKeyAsync(UserId, ItemKeys.PackStriker, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);

        var act = () => CreateHandler().Handle(
            new SelectAvatarCommand("avatar_male_pack_striker"), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ConflictException>();
        ex.Which.Code.Should().Be("AVATAR_LOCKED");

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ThrowsConflict_WhenAvatarRequiresPackUserOwnsWithZeroQuantity()
    {
        _inventoryRepository
            .Setup(r => r.GetByUserIdAndItemKeyAsync(UserId, ItemKeys.PackStriker, It.IsAny<CancellationToken>()))
            .ReturnsAsync(InventoryItem.Create(UserId, ItemKeys.PackStriker, 0));

        var act = () => CreateHandler().Handle(
            new SelectAvatarCommand("avatar_male_pack_striker"), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ConflictException>();
        ex.Which.Code.Should().Be("AVATAR_LOCKED");
    }

    [Fact]
    public async Task Succeeds_WhenAvatarRequiresPackUserOwns()
    {
        var user = SetupExistingUser();
        _inventoryRepository
            .Setup(r => r.GetByUserIdAndItemKeyAsync(UserId, ItemKeys.PackStriker, It.IsAny<CancellationToken>()))
            .ReturnsAsync(InventoryItem.Create(UserId, ItemKeys.PackStriker, 1));

        await CreateHandler().Handle(new SelectAvatarCommand("avatar_male_pack_striker"), CancellationToken.None);

        user.SelectedAvatarKey.Should().Be("avatar_male_pack_striker");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // RN-005: o mesmo pack libera o avatar feminino equivalente tambem.
    [Fact]
    public async Task Succeeds_WhenFemaleAvatarRequiresPackUserOwns()
    {
        var user = SetupExistingUser();
        _inventoryRepository
            .Setup(r => r.GetByUserIdAndItemKeyAsync(UserId, ItemKeys.PackStriker, It.IsAny<CancellationToken>()))
            .ReturnsAsync(InventoryItem.Create(UserId, ItemKeys.PackStriker, 1));

        await CreateHandler().Handle(new SelectAvatarCommand("avatar_female_pack_striker"), CancellationToken.None);

        user.SelectedAvatarKey.Should().Be("avatar_female_pack_striker");
    }
}
