using Nexora.Application.Tenants.Support;
using Nexora.Domain.Platform;
using FluentAssertions;
using Xunit;

namespace Nexora.UnitTests.Tenants;

/// <summary>
/// US-151 §12 "Unitário: ... ordenação" — <see cref="TenantAttentionRanking"/> é o ranking de
/// criticidade por trás de <c>sort=attention</c> (default do diretório). Mesma semântica de
/// <c>GetPlatformSummaryQueryHandler</c>: só SUSPENDED exige ação do administrador.
/// </summary>
public sealed class TenantAttentionRankingTests
{
    [Fact]
    public void Suspended_E_O_Mais_Critico()
    {
        TenantAttentionRanking.RankOf(TenantStatus.Suspended).Should().Be(0);
    }

    [Fact]
    public void Ranking_Segue_A_Ordem_Suspended_Provisioned_Installing_Active_Cancelled()
    {
        var suspended = TenantAttentionRanking.RankOf(TenantStatus.Suspended);
        var provisioned = TenantAttentionRanking.RankOf(TenantStatus.Provisioned);
        var installing = TenantAttentionRanking.RankOf(TenantStatus.Installing);
        var active = TenantAttentionRanking.RankOf(TenantStatus.Active);
        var cancelled = TenantAttentionRanking.RankOf(TenantStatus.Cancelled);

        suspended.Should().BeLessThan(provisioned);
        provisioned.Should().BeLessThan(installing);
        installing.Should().BeLessThan(active);
        active.Should().BeLessThan(cancelled);
    }

    [Fact]
    public void Ordenar_Uma_Lista_Por_Rank_Traz_Suspended_Primeiro()
    {
        var statuses = new[]
        {
            TenantStatus.Cancelled, TenantStatus.Active, TenantStatus.Suspended,
            TenantStatus.Installing, TenantStatus.Provisioned,
        };

        var ordered = statuses.OrderBy(TenantAttentionRanking.RankOf).ToList();

        ordered.Should().Equal(
            TenantStatus.Suspended, TenantStatus.Provisioned, TenantStatus.Installing,
            TenantStatus.Active, TenantStatus.Cancelled);
    }
}
