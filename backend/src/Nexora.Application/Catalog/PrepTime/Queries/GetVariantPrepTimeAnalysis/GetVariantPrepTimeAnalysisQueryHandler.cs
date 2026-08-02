using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Contracts.Catalog;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Catalog.PrepTime.Queries.GetVariantPrepTimeAnalysis;

/// <summary>
/// US-016 — compara o tempo de preparo cadastrado (<c>ProductVariant.PrepMinutes</c>) com o
/// tempo real observado em <c>MetricProductDaily</c> nos últimos <see cref="AnalysisWindowDays"/>
/// dias, e resolve os limiares efetivos de atenção/crítico (próprios da variação, ou herdados do
/// padrão do tenant via <see cref="TenantPrepTimeDefaults"/>).
/// </summary>
/// <remarks>
/// Constantes documentadas aqui porque nenhuma delas tem hoje uma origem de configuração real
/// (nem por tenant, nem por produto) — decisão de produto ainda não tomada, mesma situação de
/// <see cref="TenantPrepTimeDefaults"/>:
///
/// <list type="bullet">
/// <item><description>
/// <see cref="AnalysisWindowDays"/> = 30 — vem literalmente do critério de aceite "Comparativo
/// estimado versus real" do documento da US-016 ("tempo real médio... nos últimos 30 dias").
/// Corridos, não dias úteis — <c>MetricProductDaily.BusinessDay</c> já é o dia operacional
/// materializado (ADR-018), então "30 dias" aqui significa 30 valores de <c>business_day</c>,
/// não 30 dias corridos de calendário civil se a loja fechar algum dia da semana.
/// </description></item>
/// <item><description>
/// <see cref="MinimumSampleSize"/> = 20 — [HIPÓTESE]. O documento da US pede "amostra suficiente"
/// (§12, estratégia de teste) sem definir o número. 20 pedidos é um piso arbitrário mas razoável
/// para não sugerir ajuste a partir de 1-2 vendas isoladas (variância alta demais para um produto
/// de baixo giro). Recalibrar com dado real do piloto.
/// </description></item>
/// <item><description>
/// <see cref="DivergenceThreshold"/> = 20% — pedido explicitamente pela tarefa desta US
/// ("defina um limiar razoável, ex. &gt;20%") como a magnitude de divergência entre
/// <c>ActualAvgMinutes</c> e <c>ConfiguredMinutes</c> que passa a gerar sugestão de ajuste.
/// Abaixo disso, ruído normal de operação (variação de fila, pico de horário) não deveria
/// disparar alerta de recalibração toda hora.
/// </description></item>
/// </list>
///
/// [PENDÊNCIA] <c>ActualP90Minutes</c> sempre nulo — ver docstring de
/// <see cref="Nexora.Contracts.Catalog.PrepTimeAnalysisResponse"/>: não existe coluna de p90 por
/// variação em <c>metric_product_daily</c> hoje.
/// </remarks>
internal sealed class GetVariantPrepTimeAnalysisQueryHandler
    : IRequestHandler<GetVariantPrepTimeAnalysisQuery, Result<PrepTimeAnalysisResponse>>
{
    internal const int AnalysisWindowDays = 30;
    internal const int MinimumSampleSize = 20;
    internal const decimal DivergenceThreshold = 0.20m;

    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public GetVariantPrepTimeAnalysisQueryHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PrepTimeAnalysisResponse>> Handle(
        GetVariantPrepTimeAnalysisQuery request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result<PrepTimeAnalysisResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado à requisição.",
                ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;

        var variant = await _db.ProductVariants.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == request.VariantId && v.TenantId == tenantId, cancellationToken);

        if (variant is null)
        {
            return Result<PrepTimeAnalysisResponse>.Failure("Variação não encontrada.", ApiErrorCodes.PrepTimeVariantNotFound);
        }

        var tenantConfig = await _db.TenantConfigs.AsNoTracking()
            .FirstOrDefaultAsync(c => c.TenantId == tenantId, cancellationToken);

        var (defaultWarn, defaultCritical) = TenantPrepTimeDefaults.Resolve(tenantConfig?.Thresholds);
        var effectiveWarn = variant.WarnMinutes ?? defaultWarn;
        var effectiveCritical = variant.CriticalMinutes ?? defaultCritical;

        // Agregado por TENANT (não por loja): o cardápio — e o tempo de preparo cadastrado — é
        // dado de nuvem compartilhado entre todas as lojas do estabelecimento (cabeçalho "Autoridade
        // do dado: Nuvem" do documento da US); somar entre lojas dá a amostra real mais completa.
        var windowStart = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime).AddDays(-AnalysisWindowDays);

        var dailyMetrics = await _db.MetricProductDailies.AsNoTracking()
            .Where(m => m.TenantId == tenantId
                && m.VariantId == request.VariantId
                && m.BusinessDay >= windowStart
                && m.AvgPrepSeconds != null)
            .Select(m => new { m.Quantity, m.AvgPrepSeconds })
            .ToListAsync(cancellationToken);

        var sampleSize = dailyMetrics.Sum(m => m.Quantity);
        decimal? actualAvgMinutes = null;
        short? suggestion = null;
        string? note = null;

        if (sampleSize > 0)
        {
            var weightedSeconds = dailyMetrics.Sum(m => (decimal)m.AvgPrepSeconds!.Value * m.Quantity);
            actualAvgMinutes = Math.Round(weightedSeconds / sampleSize / 60m, 1, MidpointRounding.AwayFromZero);

            if (sampleSize >= MinimumSampleSize && variant.PrepMinutes > 0)
            {
                var divergence = Math.Abs(actualAvgMinutes.Value - variant.PrepMinutes) / variant.PrepMinutes;
                if (divergence > DivergenceThreshold)
                {
                    suggestion = (short)Math.Round(actualAvgMinutes.Value, MidpointRounding.AwayFromZero);
                }
            }
            else if (sampleSize < MinimumSampleSize)
            {
                note = $"Amostra de {sampleSize} pedido(s) nos últimos {AnalysisWindowDays} dias — abaixo do mínimo de {MinimumSampleSize} para sugerir ajuste.";
            }
        }
        else
        {
            note = $"Sem histórico de preparo nos últimos {AnalysisWindowDays} dias.";
        }

        return Result<PrepTimeAnalysisResponse>.Success(new PrepTimeAnalysisResponse(
            variant.Id,
            variant.PrepMinutes,
            effectiveWarn,
            variant.WarnMinutes is null,
            effectiveCritical,
            variant.CriticalMinutes is null,
            actualAvgMinutes,
            ActualP90Minutes: null,
            sampleSize,
            suggestion,
            note));
    }
}
