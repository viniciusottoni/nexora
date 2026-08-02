using Awaken.Application.Common.Interfaces;
using Awaken.Application.Nutrition.Queries.GetBasicNutritionToday;
using Awaken.Contracts.Nutrition;
using Awaken.Domain.Entities.Onboarding;
using Awaken.Domain.Repositories;
using FluentAssertions;
using Moq;

namespace Awaken.UnitTests.Nutrition;

public class GetBasicNutritionTodayQueryHandlerTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateOnly Today = new(2026, 6, 28);
    private static readonly DateTime NowLocal = new(2026, 6, 28, 12, 0, 0); // meio-dia

    private readonly Mock<ICurrentUserService> _currentUserService = new();
    private readonly Mock<IUserProfileRepository> _profileRepository = new();
    private readonly Mock<INutritionLogRepository> _nutritionLogRepository = new();
    private readonly Mock<IUserDateService> _userDateService = new();
    private readonly Mock<IUserNutritionPreferenceRepository> _preferenceRepository = new();

    public GetBasicNutritionTodayQueryHandlerTests()
    {
        _currentUserService.Setup(s => s.UserId).Returns(UserId);
        _userDateService.Setup(s => s.TodayLocal).Returns(Today);
        _userDateService.Setup(s => s.NowLocal).Returns(NowLocal);
        // US-090: sem preferência por padrão → usa 250 ml.
        _preferenceRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Awaken.Domain.Entities.Nutrition.UserNutritionPreference?)null);
    }

    private GetBasicNutritionTodayQueryHandler CreateHandler() => new(
        _currentUserService.Object,
        _profileRepository.Object,
        _nutritionLogRepository.Object,
        _userDateService.Object,
        _preferenceRepository.Object);

    [Fact]
    public async Task ReturnsRoundedGoalsAndUsesLocalDate()
    {
        var profile = UserProfile.Create(UserId, weightKg: 70.4m);
        _profileRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _nutritionLogRepository
            .Setup(r => r.GetByUserIdAndDateAsync(UserId, Today, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Awaken.Domain.Entities.Nutrition.NutritionLog?)null);

        var result = await CreateHandler().Handle(new GetBasicNutritionTodayQuery(), CancellationToken.None);

        result.WaterMinimumMl.Should().Be(2112);
        result.WaterIdealMl.Should().Be(3520);
        result.WaterConsumedMl.Should().Be(0);
        _nutritionLogRepository.Verify(
            r => r.GetByUserIdAndDateAsync(UserId, Today, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ReturnsZeroGoalsWhenWeightIsMissing()
    {
        var profile = UserProfile.Create(UserId, weightKg: null);
        _profileRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _nutritionLogRepository
            .Setup(r => r.GetByUserIdAndDateAsync(UserId, Today, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Awaken.Domain.Entities.Nutrition.NutritionLog?)null);

        var result = await CreateHandler().Handle(new GetBasicNutritionTodayQuery(), CancellationToken.None);

        result.Should().Be(new BasicNutritionTodayResponse(0, 0, 0));
    }

    // ── US-088 unit tests ──────────────────────────────────────────────────────

    [Fact]
    public void CalculateCalories_Male_ReturnsEstimate()
    {
        var profile = UserProfile.Create(UserId,
            weightKg: 70m, heightCm: 175m, age: 30,
            biologicalSex: "masculino", availableDaysPerWeek: 3);

        var now = new DateTime(2026, 6, 28, 12, 0, 0); // meio-dia → 50% do dia
        var (today, untilNow, status) = GetBasicNutritionTodayQueryHandler.CalculateCalories(profile, now);

        status.Should().Be("estimated");
        // TMB = 10*70 + 6.25*175 - 5*30 + 5 = 700 + 1093.75 - 150 + 5 = 1648.75
        // Fator atividade 3d/week → 1.55; body type null → 1.00
        // Daily ≈ 1648.75 * 1.55 * 1.00 ≈ 2555.5625 → arredonda para 2556
        // Meio-dia = 720 min / 1440 min = 50% → untilNow ≈ 1278
        today.Should().Be(2556);
        untilNow.Should().Be(1278);
    }

    [Fact]
    public void CalculateCalories_Female_ReturnsEstimate()
    {
        var profile = UserProfile.Create(UserId,
            weightKg: 60m, heightCm: 165m, age: 25,
            biologicalSex: "feminino", availableDaysPerWeek: 2);

        var now = new DateTime(2026, 6, 28, 0, 0, 0); // meia-noite → 0% do dia
        var (today, untilNow, status) = GetBasicNutritionTodayQueryHandler.CalculateCalories(profile, now);

        status.Should().Be("estimated");
        // TMB = 10*60 + 6.25*165 - 5*25 - 161 = 600 + 1031.25 - 125 - 161 = 1345.25
        // Fator atividade 2d/week → 1.35; body type null → 1.00
        // Daily ≈ 1345.25 * 1.35 ≈ 1816.0875 → 1816
        // Meia-noite = 0 minutos → untilNow = 0
        today.Should().Be(1816);
        untilNow.Should().Be(0);
    }

    [Fact]
    public void CalculateCalories_AthleteBodyType_AppliesFactor()
    {
        var profile = UserProfile.Create(UserId,
            weightKg: 80m, heightCm: 180m, age: 28,
            biologicalSex: "male", availableDaysPerWeek: 5, bodyType: "athletic_strong");

        var now = new DateTime(2026, 6, 28, 23, 59, 0); // fim do dia
        var (today, _, status) = GetBasicNutritionTodayQueryHandler.CalculateCalories(profile, now);

        status.Should().Be("estimated");
        // TMB = 10*80 + 6.25*180 - 5*28 + 5 = 800 + 1125 - 140 + 5 = 1790
        // Fator 5d → 1.75; athletic_strong → 1.07
        // Daily ≈ 1790 * 1.75 * 1.07 ≈ 3352.225 → 3352
        today.Should().Be(3352);
    }

    [Fact]
    public void CalculateCalories_OverweightBodyType_AppliesFactor()
    {
        var profile = UserProfile.Create(UserId,
            weightKg: 90m, heightCm: 170m, age: 40,
            biologicalSex: "masculino", availableDaysPerWeek: 1, bodyType: "overweight");

        var now = new DateTime(2026, 6, 28, 12, 0, 0);
        var (today, _, status) = GetBasicNutritionTodayQueryHandler.CalculateCalories(profile, now);

        status.Should().Be("estimated");
        // TMB = 10*90 + 6.25*170 - 5*40 + 5 = 900 + 1062.5 - 200 + 5 = 1767.5
        // Fator 1d → 1.35; overweight → 0.92
        // Daily = 1767.5 * 1.35 * 0.92 = 2195.235 → 2195
        today.Should().Be(2195);
    }

    [Fact]
    public void CalculateCalories_SedentaryNoTrainingDays_UsesLowestFactor()
    {
        var profile = UserProfile.Create(UserId,
            weightKg: 70m, heightCm: 170m, age: 35,
            biologicalSex: "feminino", availableDaysPerWeek: 0);

        var now = new DateTime(2026, 6, 28, 12, 0, 0);
        var (today, _, status) = GetBasicNutritionTodayQueryHandler.CalculateCalories(profile, now);

        status.Should().Be("estimated");
        // TMB = 10*70 + 6.25*170 - 5*35 - 161 = 700 + 1062.5 - 175 - 161 = 1426.5
        // Fator 0d → 1.20; normal → 1.00
        // Daily ≈ 1426.5 * 1.20 ≈ 1711.8 → 1712
        today.Should().Be(1712);
    }

    [Fact]
    public void CalculateCalories_AthleteSevenDays_UsesHighestFactor()
    {
        var profile = UserProfile.Create(UserId,
            weightKg: 75m, heightCm: 178m, age: 22,
            biologicalSex: "male", availableDaysPerWeek: 7);

        var now = new DateTime(2026, 6, 28, 12, 0, 0);
        var (today, _, status) = GetBasicNutritionTodayQueryHandler.CalculateCalories(profile, now);

        status.Should().Be("estimated");
        // Fator 7d → 1.90
        today.Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData("masculino", true)]
    [InlineData("male", true)]
    [InlineData("m", true)]
    [InlineData("homem", true)]
    [InlineData("feminino", false)]
    [InlineData("female", false)]
    [InlineData("f", false)]
    [InlineData("mulher", false)]
    public void InterpretBiologicalSex_KnownValues(string value, bool expectedMale)
    {
        GetBasicNutritionTodayQueryHandler.InterpretBiologicalSex(value)
            .Should().Be(expectedMale);
    }

    [Theory]
    [InlineData("não-binário")]
    [InlineData("other")]
    [InlineData("")]
    [InlineData(null)]
    public void InterpretBiologicalSex_UnknownOrEmpty_ReturnsNull(string? value)
    {
        GetBasicNutritionTodayQueryHandler.InterpretBiologicalSex(value)
            .Should().BeNull();
    }

    [Fact]
    public void CalculateCalories_MissingSex_ReturnsIncomplete()
    {
        var profile = UserProfile.Create(UserId,
            weightKg: 70m, heightCm: 170m, age: 30,
            biologicalSex: null);

        var (_, _, status) = GetBasicNutritionTodayQueryHandler.CalculateCalories(profile, DateTime.UtcNow);

        status.Should().Be("incomplete");
    }

    [Fact]
    public void CalculateCalories_UninterpretableSex_ReturnsIncomplete()
    {
        var profile = UserProfile.Create(UserId,
            weightKg: 70m, heightCm: 170m, age: 30,
            biologicalSex: "não-binário");

        var (_, _, status) = GetBasicNutritionTodayQueryHandler.CalculateCalories(profile, DateTime.UtcNow);

        status.Should().Be("incomplete");
    }

    [Fact]
    public void CalculateCalories_MissingAge_ReturnsIncomplete()
    {
        var profile = UserProfile.Create(UserId,
            weightKg: 70m, heightCm: 170m, age: null,
            biologicalSex: "masculino");

        var (_, _, status) = GetBasicNutritionTodayQueryHandler.CalculateCalories(profile, DateTime.UtcNow);

        status.Should().Be("incomplete");
    }

    [Fact]
    public void CalculateCalories_MissingHeight_ReturnsIncomplete()
    {
        var profile = UserProfile.Create(UserId,
            weightKg: 70m, heightCm: null, age: 30,
            biologicalSex: "masculino");

        var (_, _, status) = GetBasicNutritionTodayQueryHandler.CalculateCalories(profile, DateTime.UtcNow);

        status.Should().Be("incomplete");
    }

    [Fact]
    public void CalculateCalories_StartOfDay_UntilNowIsZero()
    {
        var profile = UserProfile.Create(UserId,
            weightKg: 70m, heightCm: 175m, age: 30,
            biologicalSex: "masculino", availableDaysPerWeek: 3);

        var midnight = new DateTime(2026, 6, 28, 0, 0, 0);
        var (_, untilNow, _) = GetBasicNutritionTodayQueryHandler.CalculateCalories(profile, midnight);

        untilNow.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithCompleteProfile_ReturnsCaloriesEstimated()
    {
        var profile = UserProfile.Create(UserId,
            weightKg: 70m, heightCm: 175m, age: 30,
            biologicalSex: "masculino", availableDaysPerWeek: 3);
        _profileRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _nutritionLogRepository
            .Setup(r => r.GetByUserIdAndDateAsync(UserId, Today, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Awaken.Domain.Entities.Nutrition.NutritionLog?)null);

        var result = await CreateHandler().Handle(new GetBasicNutritionTodayQuery(), CancellationToken.None);

        result.CaloriesCalculationStatus.Should().Be("estimated");
        result.CaloriesSpentEstimatedToday.Should().BeGreaterThan(0);
        result.CaloriesSpentEstimatedUntilNow.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Handle_WithIncompleteProfile_ReturnsCaloriesIncomplete()
    {
        // Só tem peso (suficiente para água), mas não sexo/altura/idade.
        var profile = UserProfile.Create(UserId, weightKg: 70m);
        _profileRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _nutritionLogRepository
            .Setup(r => r.GetByUserIdAndDateAsync(UserId, Today, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Awaken.Domain.Entities.Nutrition.NutritionLog?)null);

        var result = await CreateHandler().Handle(new GetBasicNutritionTodayQuery(), CancellationToken.None);

        // Água funciona (tem peso)
        result.WaterMinimumMl.Should().BeGreaterThan(0);
        // Calorias incompleta (sem sexo/idade/altura)
        result.CaloriesCalculationStatus.Should().Be("incomplete");
        result.CaloriesSpentEstimatedToday.Should().Be(0);
    }

    // ── US-090 unit tests ──────────────────────────────────────────────────────

    [Fact]
    public async Task DefaultsCupVolumeToTwoFiftyWhenNoPreference()
    {
        var profile = UserProfile.Create(UserId, weightKg: 70m);
        _profileRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _nutritionLogRepository
            .Setup(r => r.GetByUserIdAndDateAsync(UserId, Today, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Awaken.Domain.Entities.Nutrition.NutritionLog?)null);
        // _preferenceRepository already returns null in constructor.

        var result = await CreateHandler().Handle(new GetBasicNutritionTodayQuery(), CancellationToken.None);

        result.CupVolumeMl.Should().Be(250);
    }

    [Fact]
    public async Task ReturnsPreferenceCupVolumeWhenSet()
    {
        var profile = UserProfile.Create(UserId, weightKg: 70m);
        _profileRepository.Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _nutritionLogRepository
            .Setup(r => r.GetByUserIdAndDateAsync(UserId, Today, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Awaken.Domain.Entities.Nutrition.NutritionLog?)null);
        var pref = Awaken.Domain.Entities.Nutrition.UserNutritionPreference.Create(UserId, 500);
        _preferenceRepository
            .Setup(r => r.GetByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pref);

        var result = await CreateHandler().Handle(new GetBasicNutritionTodayQuery(), CancellationToken.None);

        result.CupVolumeMl.Should().Be(500);
    }
}
