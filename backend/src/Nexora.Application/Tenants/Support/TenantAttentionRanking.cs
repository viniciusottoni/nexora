using Nexora.Domain.Platform;

namespace Nexora.Application.Tenants.Support;

/// <summary>
/// "Criticidade" do diretório de estabelecimentos (US-151 §7/§15, <c>sort=attention</c>, também o
/// default) — mesma semântica já usada em <c>GetPlatformSummaryQueryHandler</c>: SUSPENDED é o
/// único status que exige ação do administrador de plataforma. PROVISIONED/INSTALLING (US-153) são
/// fluxo normal de implantação, só um pouco mais urgentes de acompanhar que ACTIVE; Cancelled já
/// está encerrado (nada a fazer).
/// </summary>
/// <remarks>
/// Fonte da verdade PURA (testável sem banco) da regra de ranking — mas
/// <c>ListTenantsQueryHandler</c> não pode chamar este método dentro da query LINQ-to-Entities
/// (EF Core não traduz chamada a método arbitrário para SQL): a query duplica esta MESMA lógica
/// como expressão condicional inline (<c>t.Status == ... ? 0 : ...</c>), a única forma de o Npgsql
/// provider gerar um <c>CASE WHEN</c>. Este método é usado depois de materializar a linha (cursor:
/// <c>BuildPrimary</c>) e por <c>Nexora.UnitTests.Tenants.TenantAttentionRankingTests</c> — qualquer
/// mudança de ranking precisa espelhar as duas cópias.
/// </remarks>
public static class TenantAttentionRanking
{
    public static int RankOf(TenantStatus status) => status switch
    {
        TenantStatus.Suspended => 0,
        TenantStatus.Provisioned => 1,
        TenantStatus.Installing => 2,
        TenantStatus.Active => 3,
        TenantStatus.Cancelled => 4,
        _ => 5
    };
}
