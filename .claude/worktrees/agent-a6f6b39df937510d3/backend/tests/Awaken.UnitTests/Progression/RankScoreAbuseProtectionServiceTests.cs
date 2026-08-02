using Awaken.Application.Progression.Services;
using FluentAssertions;

namespace Awaken.UnitTests.Progression;

/// US-155: testes da lógica de detecção de abuso de RankScore.
public class RankScoreAbuseProtectionServiceTests
{
    // CA-001 / RN-003: dor forte → sem RankScore, abuso sinalizado.
    [Fact]
    public void Evaluate_StrongPain_ReturnsZeroMultiplierAndFlagsAbuse()
    {
        var result = RankScoreAbuseProtectionService.Evaluate(
            strongPainReported: true, setsCompleted: 10, totalSets: 10);

        result.Multiplier.Should().Be(0.0m);
        result.AbuseSuspected.Should().BeTrue();
    }

    // RN-003: dor forte tem prioridade sobre qualquer outra condição.
    [Fact]
    public void Evaluate_StrongPainWithFullCompletion_StillReturnsZeroMultiplier()
    {
        var result = RankScoreAbuseProtectionService.Evaluate(
            strongPainReported: true, setsCompleted: 10, totalSets: 10);

        result.Multiplier.Should().Be(0.0m);
    }

    // CA-001 / RN-004: conclusão < 50% → ganho reduzido a 50%.
    [Theory]
    [InlineData(0, 10)]   // 0% completado
    [InlineData(1, 10)]   // 10% completado
    [InlineData(4, 10)]   // 40% completado
    [InlineData(3, 8)]    // 37.5% completado
    public void Evaluate_LowCompletionRatio_ReturnsHalfMultiplierAndFlagsAbuse(
        int setsCompleted, int totalSets)
    {
        var result = RankScoreAbuseProtectionService.Evaluate(
            strongPainReported: false, setsCompleted: setsCompleted, totalSets: totalSets);

        result.Multiplier.Should().Be(0.5m);
        result.AbuseSuspected.Should().BeTrue();
    }

    // Exatamente 50% de completude: não é abuso.
    [Theory]
    [InlineData(5, 10)]   // 50% exato
    [InlineData(1, 2)]    // 50% exato
    public void Evaluate_ExactlyHalfCompletion_IsNotConsideredAbuse(int setsCompleted, int totalSets)
    {
        var result = RankScoreAbuseProtectionService.Evaluate(
            strongPainReported: false, setsCompleted: setsCompleted, totalSets: totalSets);

        result.Multiplier.Should().Be(1.0m);
        result.AbuseSuspected.Should().BeFalse();
    }

    // Completude normal (≥ 50%): ganho pleno, sem abuso.
    [Theory]
    [InlineData(5, 10)]   // 50%
    [InlineData(8, 10)]   // 80%
    [InlineData(10, 10)]  // 100%
    [InlineData(3, 4)]    // 75%
    public void Evaluate_NormalCompletion_ReturnsFullMultiplierNoAbuse(int setsCompleted, int totalSets)
    {
        var result = RankScoreAbuseProtectionService.Evaluate(
            strongPainReported: false, setsCompleted: setsCompleted, totalSets: totalSets);

        result.Multiplier.Should().Be(1.0m);
        result.AbuseSuspected.Should().BeFalse();
    }

    // RN-006: proteção não depende de dor para funcionar — completude normal sem dor = pleno.
    [Fact]
    public void Evaluate_NoPainFullCompletion_NoAbuseSuspected()
    {
        var result = RankScoreAbuseProtectionService.Evaluate(
            strongPainReported: false, setsCompleted: 10, totalSets: 10);

        result.AbuseSuspected.Should().BeFalse();
        result.Multiplier.Should().Be(1.0m);
    }

    // Edge case: totalSets=0 não lança exceção; tratado como completude zero.
    [Fact]
    public void Evaluate_ZeroTotalSets_TreatedAsZeroCompletion()
    {
        var result = RankScoreAbuseProtectionService.Evaluate(
            strongPainReported: false, setsCompleted: 0, totalSets: 0);

        result.Multiplier.Should().Be(0.5m);
        result.AbuseSuspected.Should().BeTrue();
    }
}
