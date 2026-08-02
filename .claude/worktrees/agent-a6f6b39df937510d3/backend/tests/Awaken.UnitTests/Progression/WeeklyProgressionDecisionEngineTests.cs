using Awaken.Domain.Entities.Quests;
using Awaken.Domain.Services.Progression;
using FluentAssertions;
using Xunit;

namespace Awaken.UnitTests.Progression;

public class WeeklyProgressionDecisionEngineTests
{
    private static WeeklyProgressionDecisionRequest BaseRequest(
        IReadOnlyList<string> feelings, int mesocycleWeek = 2, int consecutiveHard = 0,
        int currentVolume = 0, string? lastAxis = null, IReadOnlyDictionary<string, int>? attributes = null) =>
        new(feelings, ConsecutiveEasyWeeks: 0, consecutiveHard, mesocycleWeek, currentVolume,
            Rank: "D", attributes ?? new Dictionary<string, int>(), lastAxis);

    [Fact] // CA-001
    public void Decide_ProgressesExactlyOneAxisWhenTooEasy()
    {
        var request = BaseRequest([PerceivedFeelings.TooEasy, PerceivedFeelings.TooEasy]);
        var decision = WeeklyProgressionDecisionEngine.Decide(request);

        decision.Decision.Should().Be("progress");
        decision.Axis.Should().Be("reps"); // primeiro eixo do ciclo quando não há LastAxis
    }

    [Fact] // CA-002
    public void Decide_HoldsOrRegressesWithoutAddingOverloadWhenTooHard()
    {
        var request = BaseRequest([PerceivedFeelings.TooHard], currentVolume: 2);
        var decision = WeeklyProgressionDecisionEngine.Decide(request);

        decision.Decision.Should().Be("regress");
        decision.VolumeSetsDelta.Should().BeLessThanOrEqualTo(2);
        decision.RpeDelta.Should().BeLessThanOrEqualTo(0);
    }

    [Fact] // CA-003
    public void Decide_TriggersDeloadAfterFiveConsecutiveWeeks()
    {
        var request = BaseRequest([PerceivedFeelings.TooEasy], mesocycleWeek: 5, currentVolume: 4);
        var decision = WeeklyProgressionDecisionEngine.Decide(request);

        decision.Decision.Should().Be("deload");
        decision.DeloadWeek.Should().BeTrue();
        decision.VolumeSetsDelta.Should().BeLessThan(4);
    }

    [Fact] // CA-003 (gatilho alternativo)
    public void Decide_TriggersDeloadAfterTwoConsecutiveHardWeeks()
    {
        var request = BaseRequest([PerceivedFeelings.TooHard], consecutiveHard: 2);
        var decision = WeeklyProgressionDecisionEngine.Decide(request);

        decision.Decision.Should().Be("deload");
    }

    [Fact] // RN (10.4) dados insuficientes
    public void Decide_HoldsWhenNoFeelingDataAvailable()
    {
        var decision = WeeklyProgressionDecisionEngine.Decide(BaseRequest([]));

        decision.Decision.Should().Be("hold");
        decision.Axis.Should().BeNull();
    }

    [Fact] // RN-003: só 1 eixo por vez, avança o ciclo a partir do último usado
    public void Decide_AdvancesToNextAxisInCycleWhenProgressingRepeatedly()
    {
        var decision = WeeklyProgressionDecisionEngine.Decide(
            BaseRequest([PerceivedFeelings.TooEasy], lastAxis: "reps"));

        decision.Axis.Should().Be("add_set");
    }

    [Fact] // RN-004: teto de volume -> pula add_set e avança pro próximo eixo
    public void Decide_SkipsAddSetAxisWhenVolumeCapReached()
    {
        var decision = WeeklyProgressionDecisionEngine.Decide(
            BaseRequest([PerceivedFeelings.TooEasy], lastAxis: "reps", currentVolume: 4));

        decision.Axis.Should().Be("harder_variant");
        decision.VolumeSetsDelta.Should().Be(4); // não ultrapassa o teto
    }

    [Fact] // CA-005
    public void Decide_HighStrengthGetsAttributeBiasTowardHeavierVariants()
    {
        var withHighStrength = WeeklyProgressionDecisionEngine.Decide(
            BaseRequest([PerceivedFeelings.TooEasy], attributes: new Dictionary<string, int> { ["strength"] = 8 }));
        var withLowStrength = WeeklyProgressionDecisionEngine.Decide(
            BaseRequest([PerceivedFeelings.TooEasy], attributes: new Dictionary<string, int> { ["strength"] = 1 }));

        withHighStrength.AttributeBias.Should().ContainKey("strength");
        withLowStrength.AttributeBias.Should().NotContainKey("strength");
    }

    [Fact] // RN-011: regressão não deve carregar sinal de deload (é ajuste de treino, não punição)
    public void Decide_RegressDoesNotIncludeDeloadFlag()
    {
        var decision = WeeklyProgressionDecisionEngine.Decide(BaseRequest([PerceivedFeelings.TooHard]));
        decision.DeloadWeek.Should().BeFalse();
    }

    [Fact] // Determinismo
    public void Decide_IsDeterministic()
    {
        var request = BaseRequest([PerceivedFeelings.TooEasy, PerceivedFeelings.JustRight]);
        var first = WeeklyProgressionDecisionEngine.Decide(request);
        var second = WeeklyProgressionDecisionEngine.Decide(request);

        first.Should().BeEquivalentTo(second);
    }
}
