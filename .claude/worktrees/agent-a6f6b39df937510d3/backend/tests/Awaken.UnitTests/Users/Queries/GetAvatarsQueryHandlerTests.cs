using Awaken.Application.Common.Interfaces;
using Awaken.Application.Users.Queries.GetAvatars;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Entities.Avatars;
using Awaken.Domain.Entities.Inventory;
using Awaken.Domain.Entities.Onboarding;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Users.Queries;

public class GetAvatarsQueryHandlerTests
{
    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IUserProfileRepository> _userProfileRepository = new();
    private readonly Mock<IInventoryRepository> _inventoryRepository = new();

    private static readonly Guid UserId = Guid.NewGuid();

    public GetAvatarsQueryHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(UserId);
        _userProfileRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);
    }

    private GetAvatarsQueryHandler CreateHandler() => new(
        _currentUserService.Object,
        _userRepository.Object,
        _userProfileRepository.Object,
        _inventoryRepository.Object);

    private void SetupUser(string? selectedAvatarKey)
    {
        var user = User.Create("hunter@awaken.app", "hash", "Hunter");
        if (selectedAvatarKey is not null)
        {
            user.SelectAvatar(selectedAvatarKey, DateTime.UtcNow);
        }

        _userRepository
            .Setup(r => r.GetByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
    }

    [Fact]
    public async Task DefaultAvatarIsAlwaysUnlocked()
    {
        SetupUser(selectedAvatarKey: null);
        _inventoryRepository
            .Setup(r => r.GetByUserIdAndItemKeyAsync(UserId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);

        var result = await CreateHandler().Handle(new GetAvatarsQuery(), CancellationToken.None);

        var defaultAvatar = result.Single(a => a.AvatarKey == AvatarCatalog.DefaultAvatarKey);
        defaultAvatar.IsUnlocked.Should().BeTrue();
        defaultAvatar.RequiredItemKey.Should().BeNull();
    }

    [Fact]
    public async Task AvatarWithPackNotOwnedIsLocked()
    {
        SetupUser(selectedAvatarKey: null);
        _inventoryRepository
            .Setup(r => r.GetByUserIdAndItemKeyAsync(UserId, ItemKeys.PackStriker, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);

        var result = await CreateHandler().Handle(new GetAvatarsQuery(), CancellationToken.None);

        var lockedAvatar = result.Single(a => a.AvatarKey == "avatar_male_pack_striker");
        lockedAvatar.IsUnlocked.Should().BeFalse();
        lockedAvatar.RequiredItemKey.Should().Be(ItemKeys.PackStriker);
    }

    [Fact]
    public async Task AvatarWithPackOwnedIsUnlocked()
    {
        SetupUser(selectedAvatarKey: null);
        _inventoryRepository
            .Setup(r => r.GetByUserIdAndItemKeyAsync(UserId, ItemKeys.PackStriker, It.IsAny<CancellationToken>()))
            .ReturnsAsync(InventoryItem.Create(UserId, ItemKeys.PackStriker, 1));

        var result = await CreateHandler().Handle(new GetAvatarsQuery(), CancellationToken.None);

        result.Single(a => a.AvatarKey == "avatar_male_pack_striker").IsUnlocked.Should().BeTrue();
    }

    [Fact]
    public async Task AvatarWithPackOwnedButZeroQuantityIsLocked()
    {
        SetupUser(selectedAvatarKey: null);
        _inventoryRepository
            .Setup(r => r.GetByUserIdAndItemKeyAsync(UserId, ItemKeys.PackStriker, It.IsAny<CancellationToken>()))
            .ReturnsAsync(InventoryItem.Create(UserId, ItemKeys.PackStriker, 0));

        var result = await CreateHandler().Handle(new GetAvatarsQuery(), CancellationToken.None);

        result.Single(a => a.AvatarKey == "avatar_male_pack_striker").IsUnlocked.Should().BeFalse();
    }

    // RN-005: o mesmo pack libera a mesma tematica para os dois sexos.
    [Fact]
    public async Task SamePackUnlocksBothMaleAndFemaleAvatarOfSameTheme()
    {
        SetupUser(selectedAvatarKey: null);
        _inventoryRepository
            .Setup(r => r.GetByUserIdAndItemKeyAsync(UserId, ItemKeys.PackShadow, It.IsAny<CancellationToken>()))
            .ReturnsAsync(InventoryItem.Create(UserId, ItemKeys.PackShadow, 1));
        _inventoryRepository
            .Setup(r => r.GetByUserIdAndItemKeyAsync(
                UserId, It.Is<string>(k => k != ItemKeys.PackShadow), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);

        var result = await CreateHandler().Handle(new GetAvatarsQuery(), CancellationToken.None);

        result.Single(a => a.AvatarKey == "avatar_male_pack_shadow").IsUnlocked.Should().BeTrue();
        result.Single(a => a.AvatarKey == "avatar_female_pack_shadow").IsUnlocked.Should().BeTrue();
        result.Single(a => a.AvatarKey == "avatar_male_pack_striker").IsUnlocked.Should().BeFalse();
    }

    [Fact]
    public async Task IsSelectedMatchesDefaultAvatarWhenUserNeverSelectedManually()
    {
        SetupUser(selectedAvatarKey: null);
        _inventoryRepository
            .Setup(r => r.GetByUserIdAndItemKeyAsync(UserId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);

        var result = await CreateHandler().Handle(new GetAvatarsQuery(), CancellationToken.None);

        result.Single(a => a.AvatarKey == AvatarCatalog.DefaultAvatarKey).IsSelected.Should().BeTrue();
        result.Where(a => a.AvatarKey != AvatarCatalog.DefaultAvatarKey)
            .Should().OnlyContain(a => !a.IsSelected);
    }

    [Fact]
    public async Task IsSelectedMatchesUserSelectedAvatarKey()
    {
        SetupUser(selectedAvatarKey: "avatar_male_1");
        _inventoryRepository
            .Setup(r => r.GetByUserIdAndItemKeyAsync(UserId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);

        var result = await CreateHandler().Handle(new GetAvatarsQuery(), CancellationToken.None);

        result.Single(a => a.AvatarKey == "avatar_male_1").IsSelected.Should().BeTrue();
        result.Single(a => a.AvatarKey == AvatarCatalog.DefaultAvatarKey).IsSelected.Should().BeFalse();
    }

    [Fact]
    public async Task IsSelectedMatchesFemaleDefaultWhenBiologicalSexIsFeminino()
    {
        SetupUser(selectedAvatarKey: null);
        _userProfileRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserProfile.Create(UserId, biologicalSex: "feminino"));
        _inventoryRepository
            .Setup(r => r.GetByUserIdAndItemKeyAsync(UserId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);

        var result = await CreateHandler().Handle(new GetAvatarsQuery(), CancellationToken.None);

        result.Single(a => a.AvatarKey == AvatarCatalog.DefaultFemaleAvatarKey).IsSelected.Should().BeTrue();
        result.Single(a => a.AvatarKey == AvatarCatalog.DefaultAvatarKey).IsSelected.Should().BeFalse();
    }

    [Fact]
    public async Task IsSelectedIsNoneFromCatalogWhenUserHasGoogleAvatarAndNeverSelectedManually()
    {
        var user = User.CreateFromGoogle("hunter@awaken.app", "google-sub", "Hunter", "https://avatar.url");
        _userRepository
            .Setup(r => r.GetByIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _inventoryRepository
            .Setup(r => r.GetByUserIdAndItemKeyAsync(UserId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);

        var result = await CreateHandler().Handle(new GetAvatarsQuery(), CancellationToken.None);

        result.Should().OnlyContain(a => !a.IsSelected);
        _userProfileRepository.Verify(
            r => r.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
