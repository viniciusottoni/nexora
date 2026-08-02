using Awaken.Domain.Entities.Progression;
using FluentAssertions;

namespace Awaken.UnitTests.Domain;

public class HunterProgressionTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateTime _utcNow = new(2026, 6, 18, 10, 0, 0, DateTimeKind.Utc);

    // ── US-066 / CA-002: todo usuário começa no Level 1. ──────────────────────

    [Fact]
    public void Create_InitialState_IsLevelOneWithZeroXp()
    {
        var progression = HunterProgression.Create(_userId);
        progression.Level.Should().Be(1);
        progression.TotalXp.Should().Be(0);
        progression.XpToNextLevel.Should().Be(100);
    }

    // US-066 / CA-001: sem level-up abaixo do limiar.
    [Fact]
    public void AddXp_BelowThreshold_DoesNotLevelUp()
    {
        var progression = HunterProgression.Create(_userId);
        progression.AddXp(99, _utcNow);
        progression.Level.Should().Be(1);
        progression.TotalXp.Should().Be(99);
        progression.XpToNextLevel.Should().Be(100);
    }

    // US-066 / CA-001: level up exato no limiar, XP restante = 0.
    [Fact]
    public void AddXp_AtThreshold_LevelsUpAndResetsXp()
    {
        var progression = HunterProgression.Create(_userId);
        progression.AddXp(100, _utcNow);
        progression.Level.Should().Be(2);
        progression.TotalXp.Should().Be(0);
        progression.XpToNextLevel.Should().Be(HunterProgression.CalculateXpToNextLevel(2));
    }

    // US-066 / 9.1: XP excede o limiar — carrega o restante para o próximo nível.
    [Fact]
    public void AddXp_OverThreshold_CarriesOverRemainingXp()
    {
        var progression = HunterProgression.Create(_userId);
        progression.AddXp(150, _utcNow);
        progression.Level.Should().Be(2);
        progression.TotalXp.Should().Be(50); // 150 - 100
        progression.XpToNextLevel.Should().Be(HunterProgression.CalculateXpToNextLevel(2));
    }

    // US-066 / 9.1: múltiplos levels de uma vez.
    [Fact]
    public void AddXp_ExceedsMultipleThresholds_LevelsUpMultipleTimes()
    {
        var progression = HunterProgression.Create(_userId);
        // L1→L2: 100 XP threshold; L2→L3: floor(100*2^1.5) = 282 XP threshold.
        // Total to reach L3: 100 + 282 = 382. Adding 400 XP → L3 with 18 remaining.
        progression.AddXp(400, _utcNow);
        progression.Level.Should().Be(3);
        progression.TotalXp.Should().Be(18);
    }

    // US-066: fórmula de limiar é determinística.
    [Theory]
    [InlineData(1, 100)]
    [InlineData(2, 282)]  // floor(100 * 2^1.5) = floor(282.84) = 282
    [InlineData(3, 519)]  // floor(100 * 3^1.5) = floor(519.61) = 519
    [InlineData(4, 800)]  // floor(100 * 4^1.5) = floor(800.00) = 800
    public void CalculateXpToNextLevel_ReturnsExpectedThreshold(int level, long expectedThreshold)
    {
        HunterProgression.CalculateXpToNextLevel(level).Should().Be(expectedThreshold);
    }

    // ── US-067 / CA-001: RankScore e Rank recalculados. ──────────────────────

    [Fact]
    public void Create_InitialRankScore_IsSumOfDefaultAttributes()
    {
        var progression = HunterProgression.Create(_userId);
        // Default: todos os atributos = 1; RankScore = 6.
        progression.RankScore.Should().Be(6);
        progression.Rank.Should().Be("E");
    }

    [Fact]
    public void AddAttributePoints_UpdatesRankScoreAndPromotesRank()
    {
        var progression = HunterProgression.Create(_userId);
        // RankScore inicial = 6 (Rank E ≤17). Adicionar 12 pontos → RankScore = 18 (Rank D).
        progression.AddAttributePoints(strength: 4, agility: 2, endurance: 2, vitality: 2, focus: 1, wisdom: 1, _utcNow);
        progression.RankScore.Should().Be(18);
        progression.Rank.Should().Be("D");
    }

    // US-067: em Rank E (multiplier=1.0), RankScore acumula igual à soma dos atributos.
    [Fact]
    public void AddAttributePoints_AtRankE_RankScoreEqualsSumOfAttributes()
    {
        var progression = HunterProgression.Create(_userId);
        progression.AddAttributePoints(1, 2, 3, 4, 5, 6, _utcNow);
        var expected = progression.Strength + progression.Agility + progression.Endurance
            + progression.Vitality + progression.Focus + progression.Wisdom;
        // Multiplier E = 1.0, sem limite mensal, então efectiveGain = rawGain.
        progression.RankScore.Should().Be(expected);
    }

    // US-067 / CA-002: CA-002 — RankScore correto para onboarding cap = 48.
    [Fact]
    public void CreateFromOnboarding_SetsRankScoreAndDerivedRank()
    {
        var progression = HunterProgression.CreateFromOnboarding(
            _userId, strength: 8, agility: 8, endurance: 8, vitality: 8, focus: 8, wisdom: 8);
        progression.RankScore.Should().Be(48);
        progression.Rank.Should().Be("B"); // 48 está em B (48–83)
    }

    // US-067 / RN-005: Rank A ou superior não é atingível pelo onboarding (cap = 48 → max Rank B).
    [Fact]
    public void CreateFromOnboarding_WithCappedScore48_RankIsAtMostB()
    {
        var progression = HunterProgression.CreateFromOnboarding(
            _userId, strength: 8, agility: 8, endurance: 8, vitality: 8, focus: 8, wisdom: 8);
        progression.Rank.Should().NotBe("A");
        progression.Rank.Should().NotBe("S");
    }

    // ── US-058 / US-068: pontos de atributo. ─────────────────────────────────

    // US-058: conclusao de QuestExercise incrementa cada atributo a partir do snapshot.
    [Fact]
    public void AddAttributePoints_IncrementsEachAttributeFromKnownInitialValues()
    {
        var progression = HunterProgression.Create(_userId);
        // valores iniciais default: todos = 1

        progression.AddAttributePoints(
            strength: 2, agility: 0, endurance: 1, vitality: 0, focus: 3, wisdom: 1, _utcNow);

        progression.Strength.Should().Be(3);
        progression.Agility.Should().Be(1);
        progression.Endurance.Should().Be(2);
        progression.Vitality.Should().Be(1);
        progression.Focus.Should().Be(4);
        progression.Wisdom.Should().Be(2);
    }

    // ── US-154: diminishing returns por Rank. ────────────────────────────────

    // US-154 / CA-001: Rank B (0.90) — 10 pontos → 9 efetivos.
    // RankCalculator thresholds: ≤83=B, ≤155=A, ≤299=S, ≤587=SS.
    [Theory]
    [InlineData("B", 60, 10, 9)]    // RS=60 → Rank B (48–83). floor(10 * 0.90) = 9
    [InlineData("A", 100, 10, 8)]   // RS=100 → Rank A (84–155). floor(10 * 0.80) = 8
    [InlineData("S", 200, 10, 7)]   // RS=200 → Rank S (156–299). floor(10 * 0.70) = 7 (CA-001 da US-154)
    [InlineData("SS", 400, 10, 6)]  // RS=400 → Rank SS (300–587). floor(10 * 0.60) = 6
    public void AddAttributePoints_AtHighRank_AppliesDiminishingReturnsMultiplier(
        string expectedRank, int initialRankScore, int rawGain, int expectedEffectiveGain)
    {
        // CreateFromOnboarding sets RankScore directly (sem DR), útil para setup de testes.
        var progression = HunterProgression.CreateFromOnboarding(
            _userId, initialRankScore, 0, 0, 0, 0, 0);
        progression.Rank.Should().Be(expectedRank);

        // Grant rawGain attribute points via 1-point-per-call (each call < monthly limit).
        // Use different months to bypass monthly cap (24/month).
        var baseMonth = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var audit = progression.AddAttributePoints(rawGain, 0, 0, 0, 0, 0, baseMonth);

        audit.EffectiveGain.Should().Be(expectedEffectiveGain);
        progression.RankScore.Should().Be(initialRankScore + expectedEffectiveGain);
    }

    // US-154 / RN-003: atributos evoluem normalmente; apenas RankScore desacelera.
    [Fact]
    public void AddAttributePoints_AtRankS_AttributesIncreaseNormallyDespiteDR()
    {
        var progression = HunterProgression.CreateFromOnboarding(_userId, 200, 0, 0, 0, 0, 0);
        progression.Strength.Should().Be(200);

        progression.AddAttributePoints(strength: 5, agility: 0, endurance: 0, vitality: 0, focus: 0, wisdom: 0, _utcNow);

        progression.Strength.Should().Be(205); // atributos crescem normalmente
        progression.RankScore.Should().Be(200 + (int)Math.Floor(5 * 0.70m)); // = 203
    }

    // US-154: limite mensal de 24 RankScore — ganho acima é zerado.
    [Fact]
    public void AddAttributePoints_MonthlyLimitReached_CapsFurtherGain()
    {
        var progression = HunterProgression.Create(_userId);
        var utcNow = new DateTime(2026, 6, 18, 0, 0, 0, DateTimeKind.Utc);

        // Adiciona exatamente 24 pontos (limit = 24 no Rank E).
        progression.AddAttributePoints(24, 0, 0, 0, 0, 0, utcNow);
        progression.MonthlyRankScoreGain.Should().Be(24);
        progression.RankScore.Should().Be(6 + 24);

        // Mais pontos no mesmo mês devem ser bloqueados.
        var audit = progression.AddAttributePoints(5, 0, 0, 0, 0, 0, utcNow);

        audit.EffectiveGain.Should().Be(0);
        audit.WasMonthlyLimitApplied.Should().BeTrue();
        progression.RankScore.Should().Be(6 + 24); // sem mudança
        progression.MonthlyRankScoreGain.Should().Be(24);
    }

    // US-154: novo mês reseta o contador mensal.
    [Fact]
    public void AddAttributePoints_NewMonth_ResetsMonthlyGainCounter()
    {
        var progression = HunterProgression.Create(_userId);
        var june = new DateTime(2026, 6, 18, 0, 0, 0, DateTimeKind.Utc);
        var july = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);

        progression.AddAttributePoints(24, 0, 0, 0, 0, 0, june);
        progression.MonthlyRankScoreGain.Should().Be(24);

        progression.AddAttributePoints(10, 0, 0, 0, 0, 0, july); // novo mês

        progression.MonthlyRankScoreGain.Should().Be(10);
        progression.RankScore.Should().Be(6 + 24 + 10);
    }

    // ── US-130: XP interno de atributo. ──────────────────────────────────────

    // US-130 / RN-002 / CA-001: 8 XP + 5 XP → 1 level up, 3 XP restante.
    [Fact]
    public void AddAttributeXp_CrossingThreshold_LevelsUpAndPreservesRemainder()
    {
        var progression = HunterProgression.Create(_userId);
        // Pre-seed: 8 XP interno em Força (não gera level up).
        var noUps = progression.AddAttributeXp(strength: 8, agility: 0, endurance: 0, vitality: 0, focus: 0, wisdom: 0,
            externalMultiplier: 1.0m, utcNow: _utcNow);
        noUps.LevelUps.Strength.Should().Be(0);
        progression.StrengthXp.Should().Be(8);
        progression.Strength.Should().Be(1); // sem level up ainda

        // +5 XP → total = 13 → 1 level up, 3 restante.
        var ups = progression.AddAttributeXp(strength: 5, agility: 0, endurance: 0, vitality: 0, focus: 0, wisdom: 0,
            externalMultiplier: 1.0m, utcNow: _utcNow);

        ups.LevelUps.Strength.Should().Be(1);
        progression.Strength.Should().Be(2);
        progression.StrengthXp.Should().Be(3);
    }

    // US-130 / RN-002 / CA-002: level up de atributo atualiza RankScore.
    [Fact]
    public void AddAttributeXp_LevelUp_UpdatesRankScoreAndRank()
    {
        var progression = HunterProgression.Create(_userId);
        // 12 XP → 1 level up (10 XP consumidos), 2 restante. Strength: 1→2.
        progression.AddAttributeXp(strength: 12, agility: 0, endurance: 0, vitality: 0, focus: 0, wisdom: 0,
            externalMultiplier: 1.0m, utcNow: _utcNow);
        progression.Strength.Should().Be(2);
        progression.StrengthXp.Should().Be(2);
        progression.RankScore.Should().Be(7); // initial 6 + 1 level-up (E rank, multiplier=1.0)
    }

    // US-130 / RN-003: múltiplos level ups de uma vez.
    [Fact]
    public void AddAttributeXp_LargeGain_AppliesMultipleLevelUps()
    {
        var progression = HunterProgression.Create(_userId);
        var ups = progression.AddAttributeXp(strength: 25, agility: 0, endurance: 0, vitality: 0, focus: 0, wisdom: 0,
            externalMultiplier: 1.0m, utcNow: _utcNow);
        // 25 XP → 2 level ups (20), 5 restante.
        ups.LevelUps.Strength.Should().Be(2);
        progression.Strength.Should().Be(3);
        progression.StrengthXp.Should().Be(5);
    }

    // US-130 / 9.2: ao atingir MaxAttributeLevel (10), XP interno para de acumular.
    [Fact]
    public void AddAttributeXp_AtLevelCap_StopsAccumulatingXp()
    {
        var progression = HunterProgression.Create(_userId);
        // Elevar Força diretamente a 9 (onboarding) e depois dar XP suficiente para chegar ao cap.
        progression.AddAttributePoints(strength: 8, agility: 0, endurance: 0, vitality: 0, focus: 0, wisdom: 0, _utcNow);
        progression.Strength.Should().Be(9);

        // +10 XP → 1 level up → Level 10 (cap).
        progression.AddAttributeXp(strength: 10, agility: 0, endurance: 0, vitality: 0, focus: 0, wisdom: 0,
            externalMultiplier: 1.0m, utcNow: _utcNow);
        progression.Strength.Should().Be(HunterProgression.MaxAttributeLevel);
        progression.StrengthXp.Should().Be(0);

        // Mais XP não acumula além do cap.
        progression.AddAttributeXp(strength: 5, agility: 0, endurance: 0, vitality: 0, focus: 0, wisdom: 0,
            externalMultiplier: 1.0m, utcNow: _utcNow);
        progression.Strength.Should().Be(HunterProgression.MaxAttributeLevel);
        progression.StrengthXp.Should().Be(0);
    }

    // US-130 / RN-001: XP não gera level up abaixo do limiar.
    [Fact]
    public void AddAttributeXp_BelowThreshold_AccumulatesWithoutLevelUp()
    {
        var progression = HunterProgression.Create(_userId);
        var ups = progression.AddAttributeXp(strength: 4, agility: 2, endurance: 0, vitality: 0, focus: 0, wisdom: 1,
            externalMultiplier: 1.0m, utcNow: _utcNow);

        ups.LevelUps.Any.Should().BeFalse();
        ups.LevelUps.Strength.Should().Be(0);
        ups.LevelUps.Agility.Should().Be(0);
        ups.LevelUps.Wisdom.Should().Be(0);
        progression.Strength.Should().Be(1); // sem mudança
        progression.StrengthXp.Should().Be(4);
        progression.AgilityXp.Should().Be(2);
        progression.WisdomXp.Should().Be(1);
    }

    // US-130: AttributeLevelUpsResult.ToNameList retorna apenas atributos com level up.
    [Fact]
    public void AttributeLevelUpsResult_ToNameList_ReturnsOnlyLeveledUpAttributes()
    {
        var progression = HunterProgression.Create(_userId);
        // Seed 9 XP em Força e Agilidade.
        progression.AddAttributeXp(strength: 9, agility: 9, endurance: 0, vitality: 0, focus: 0, wisdom: 0,
            externalMultiplier: 1.0m, utcNow: _utcNow);

        // +4 XP: Força (9+4=13 → 1 up, 3 rest), Agilidade (9+1=10 → 1 up, 0 rest).
        var ups = progression.AddAttributeXp(strength: 4, agility: 1, endurance: 0, vitality: 0, focus: 0, wisdom: 0,
            externalMultiplier: 1.0m, utcNow: _utcNow);

        var names = ups.LevelUps.ToNameList();
        names.Should().Contain("strength");
        names.Should().Contain("agility");
        names.Should().NotContain("endurance");
        names.Should().NotContain("wisdom");
        names.Should().HaveCount(2);
    }

    // US-155: externalMultiplier=0.0 bloqueia ganho de RankScore mas não afeta level-ups.
    [Fact]
    public void AddAttributeXp_WithZeroExternalMultiplier_BlocksRankScoreButAllowsLevelUps()
    {
        var progression = HunterProgression.Create(_userId);
        var initialRankScore = progression.RankScore;

        var result = progression.AddAttributeXp(strength: 10, agility: 0, endurance: 0, vitality: 0, focus: 0, wisdom: 0,
            externalMultiplier: 0.0m, utcNow: _utcNow);

        // Atributo levela normalmente.
        result.LevelUps.Strength.Should().Be(1);
        progression.Strength.Should().Be(2);

        // RankScore não muda.
        result.RankScoreAudit.EffectiveGain.Should().Be(0);
        progression.RankScore.Should().Be(initialRankScore);
    }

    // US-155: externalMultiplier=0.5 reduz ganho de RankScore a metade.
    [Fact]
    public void AddAttributeXp_WithHalfExternalMultiplier_HalvesRankScoreGain()
    {
        var progression = HunterProgression.Create(_userId);
        // Rank E, externalMultiplier=0.5, rawGain=2 level-ups → floor(2 * 0.5) = 1 efetivo.
        progression.AddAttributeXp(strength: 20, agility: 0, endurance: 0, vitality: 0, focus: 0, wisdom: 0,
            externalMultiplier: 0.5m, utcNow: _utcNow);

        // 20 XP = 2 level-ups. floor(2 * 0.5) = 1 effective.
        progression.RankScore.Should().Be(6 + 1);
    }

    // US-154 / RN-005: bônus de streak também sujeito a DR em Rank S.
    [Fact]
    public void TryApplyStreakMilestoneBonus_AtRankS_AppliesSeventyPercentMultiplier()
    {
        // Rank S (RankScore=200): floor(1 * 0.70) = 0 effective for the +1 milestone.
        var progression = HunterProgression.CreateFromOnboarding(_userId, 200, 0, 0, 0, 0, 0);
        progression.Rank.Should().Be("S");

        for (var i = 0; i < 7; i++)
            progression.UpdateStreakAfterQuestCompletion(_utcNow.AddDays(i));

        var result = progression.TryApplyStreakMilestoneBonus(_utcNow.AddDays(7));

        result.RawBonus.Should().Be(1);
        result.RankScoreAudit!.EffectiveGain.Should().Be(0); // floor(1 * 0.70) = 0
        progression.StreakRankScoreBonus.Should().Be(1); // bônus bruto registrado
        progression.RankScore.Should().Be(200); // sem ganho efetivo
    }

    // US-154 / RN-005: bônus de streak em Rank E (multiplier=1.0) aplica integral.
    [Fact]
    public void TryApplyStreakMilestoneBonus_AtRankE_AppliesFullBonus()
    {
        var progression = HunterProgression.Create(_userId);

        for (var i = 0; i < 7; i++)
            progression.UpdateStreakAfterQuestCompletion(_utcNow.AddDays(i));

        var result = progression.TryApplyStreakMilestoneBonus(_utcNow.AddDays(7));

        result.RawBonus.Should().Be(1);
        result.RankScoreAudit!.EffectiveGain.Should().Be(1);
        progression.RankScore.Should().Be(6 + 1);
    }

    // ── US-132 / penalty. ────────────────────────────────────────────────────

    // US-132/CA-001: 1 dia sem completar = -10 XP, 2 dias = -20 XP, 3 dias = -30 XP.
    [Theory]
    [InlineData(1, 10)]
    [InlineData(2, 30)]
    [InlineData(3, 60)]
    public void ApplyDailyMissPenalty_IsProgressivePerConsecutiveMissedDay(
        int timesMissedInARow, long expectedTotalPenalty)
    {
        var progression = HunterProgression.Create(_userId);
        // 99 XP: fica no Level 1 (limiar = 100), então TotalXp = 99.
        progression.AddXp(99, _utcNow);

        long totalApplied = 0;
        for (var i = 0; i < timesMissedInARow; i++)
        {
            totalApplied += progression.ApplyDailyMissPenalty(_utcNow);
        }

        totalApplied.Should().Be(expectedTotalPenalty);
        progression.ConsecutiveMissedDailyDays.Should().Be(timesMissedInARow);
        progression.RecentDailyPenaltyXp.Should().Be(timesMissedInARow * 10L);
        progression.TotalXp.Should().Be(99 - expectedTotalPenalty);
    }

    // US-132/CA-002 e US-129/9.2: a penalidade nunca leva o XP abaixo de 0.
    [Fact]
    public void ApplyDailyMissPenalty_NeverTakesXpBelowZero()
    {
        var progression = HunterProgression.Create(_userId);
        progression.AddXp(5, _utcNow);

        var applied = progression.ApplyDailyMissPenalty(_utcNow);

        applied.Should().Be(5);
        progression.TotalXp.Should().Be(0);
        progression.RecentDailyPenaltyXp.Should().Be(5);
    }

    [Fact]
    public void ApplyDailyMissPenalty_AppliesNothingWhenXpAlreadyAtFloor()
    {
        var progression = HunterProgression.Create(_userId);

        var applied = progression.ApplyDailyMissPenalty(_utcNow);

        applied.Should().Be(0);
        progression.TotalXp.Should().Be(0);
        progression.RecentDailyPenaltyXp.Should().Be(0);
    }

    // US-132/RN-007: completar a daily zera o contador de dias consecutivos sem fazer.
    [Fact]
    public void UpdateStreakAfterQuestCompletion_ResetsConsecutiveMissedDailyDays()
    {
        var progression = HunterProgression.Create(_userId);
        progression.AddXp(50, _utcNow); // dentro do Level 1, sem level-up
        progression.ApplyDailyMissPenalty(_utcNow);
        progression.ApplyDailyMissPenalty(_utcNow);

        progression.UpdateStreakAfterQuestCompletion(_utcNow);

        progression.ConsecutiveMissedDailyDays.Should().Be(0);
        progression.RecentDailyPenaltyXp.Should().BeNull();
    }

    // ── US-069 / streak milestones ────────────────────────────────────────────

    // US-069 / RN-002: marcos de streak concedem bônus correto (em Rank E, sem DR).
    // US-154 RN-005: bônus de streak sujeito ao limite mensal de 24/mês.
    [Theory]
    [InlineData(7, 1)]
    [InlineData(30, 3)]
    [InlineData(90, 8)]
    [InlineData(180, 15)]
    [InlineData(365, 35)]  // rawBonus=35, mas limite mensal caps effectiveGain em 24.
    public void TryApplyStreakMilestoneBonus_AtMilestone_GrantsCorrectRawBonus(int streakDays, int expectedRawBonus)
    {
        var progression = HunterProgression.Create(_userId);
        // Simula streak chegando ao marco
        for (var i = 0; i < streakDays; i++)
        {
            progression.UpdateStreakAfterQuestCompletion(_utcNow.AddDays(i));
        }

        var result = progression.TryApplyStreakMilestoneBonus(_utcNow);

        result.RawBonus.Should().Be(expectedRawBonus);
        progression.StreakRankScoreBonus.Should().Be(expectedRawBonus);

        // Rank E (multiplier=1.0): effectiveGain = min(rawBonus, MonthlyRankScoreLimit).
        var expectedEffectiveGain = Math.Min(expectedRawBonus, HunterProgression.MonthlyRankScoreLimit);
        progression.RankScore.Should().Be(6 + expectedEffectiveGain);
    }

    // US-069 / RN-002: dia sem marco não concede bônus.
    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(8)]
    [InlineData(29)]
    [InlineData(100)]
    public void TryApplyStreakMilestoneBonus_AtNonMilestone_GrantsNothing(int streakDays)
    {
        var progression = HunterProgression.Create(_userId);
        for (var i = 0; i < streakDays; i++)
        {
            progression.UpdateStreakAfterQuestCompletion(_utcNow.AddDays(i));
        }

        var result = progression.TryApplyStreakMilestoneBonus(_utcNow);

        result.RawBonus.Should().Be(0);
        result.RankScoreAudit.Should().BeNull();
        progression.StreakRankScoreBonus.Should().Be(0);
        progression.RankScore.Should().Be(6); // sem bônus
    }

    // US-069 / RN-003: bônus de streak (efectivo) é incluído no RankScore.
    [Fact]
    public void AddAttributePoints_IncludesStreakRankScoreEffectiveGainInRankScore()
    {
        var progression = HunterProgression.Create(_userId);
        // Simula 7 dias de streak → +1 RankScore efetivo (Rank E, multiplier=1.0)
        for (var i = 0; i < 7; i++)
        {
            progression.UpdateStreakAfterQuestCompletion(_utcNow.AddDays(i));
        }
        progression.TryApplyStreakMilestoneBonus(_utcNow);
        progression.StreakRankScoreBonus.Should().Be(1);
        progression.RankScore.Should().Be(7); // 6 + 1 effective

        // Após ganhar pontos de atributo, o bônus de streak permanece no RankScore.
        progression.AddAttributePoints(1, 0, 0, 0, 0, 0, _utcNow);

        // Rank E, effectiveGain = 1. RankScore = 7 + 1 = 8.
        progression.RankScore.Should().Be(8);
    }

    // ── US-070 / ResetStreak ──────────────────────────────────────────────────

    // US-070 / RN-002: não completar a daily reinicia o streak para 0.
    [Fact]
    public void ResetStreak_SetsCurrentStreakDaysToZero()
    {
        var progression = HunterProgression.Create(_userId);
        progression.UpdateStreakAfterQuestCompletion(_utcNow.AddDays(-2));
        progression.UpdateStreakAfterQuestCompletion(_utcNow.AddDays(-1));
        progression.CurrentStreakDays.Should().Be(2);

        progression.ResetStreak(_utcNow);

        progression.CurrentStreakDays.Should().Be(0);
    }

    // US-070 / RN-002: LongestStreakDays não é afetado pelo reset.
    [Fact]
    public void ResetStreak_PreservesLongestStreakDays()
    {
        var progression = HunterProgression.Create(_userId);
        progression.UpdateStreakAfterQuestCompletion(_utcNow.AddDays(-2));
        progression.UpdateStreakAfterQuestCompletion(_utcNow.AddDays(-1));
        var longestBefore = progression.LongestStreakDays;

        progression.ResetStreak(_utcNow);

        progression.LongestStreakDays.Should().Be(longestBefore);
    }
}
