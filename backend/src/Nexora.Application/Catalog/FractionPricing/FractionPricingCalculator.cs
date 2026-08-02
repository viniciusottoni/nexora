using Nexora.Application.Abstractions.Messaging;
using Nexora.Shared.Errors;

namespace Nexora.Application.Catalog.FractionPricing;

/// <summary>
/// Regra de precificação do meio a meio (US-013 §5, RN-009 — <b>[HIPÓTESE]</b>: o padrão sugerido
/// é o maior valor entre as frações, mas as três regras precisam existir e a escolha vive em
/// <c>tenant_config.operation.halfAndHalfPricing</c>, nunca em <c>if</c> por tenant — ADR-013).
/// </summary>
public enum FractionPriceRule
{
    /// <summary>Maior preço entre as frações — padrão sugerido por RN-009.</summary>
    Highest,

    /// <summary>Média aritmética simples dos preços das frações (ignora o peso de cada uma).</summary>
    Average,

    /// <summary>Soma ponderada pelo peso de cada fração — com pesos iguais, coincide com <see cref="Average"/>.</summary>
    Proportional,
}

/// <summary>
/// Uma fração já resolvida (preço vigente já carregado) — entrada de
/// <see cref="FractionPricingCalculator.Calculate"/>. Deliberadamente só carrega os campos que a
/// regra pura de negócio precisa (nada de <c>Guid</c> de tenant, nada de EF Core): quem monta esta
/// lista (a Application, com acesso a banco) já resolveu <see cref="UnitPrice"/> via
/// <c>ChannelPriceResolver</c> e <see cref="SizeCode"/>/<see cref="FractionGroup"/> via
/// <c>ProductVariant</c>/<c>Product</c>.
/// </summary>
public sealed record FractionPricingLine(
    Guid VariantId,
    decimal Weight,
    decimal UnitPrice,
    string? SizeCode,
    string? FractionGroup);

/// <summary>Resultado de sucesso de <see cref="FractionPricingCalculator.Calculate"/> — o preço final e a regra efetivamente aplicada.</summary>
public sealed record FractionPricingCalculation(decimal UnitPrice, FractionPriceRule Rule);

/// <summary>
/// Cálculo e validação de preço de um item meio a meio (US-013) — função pura, sem I/O, coberta
/// isoladamente por <c>Nexora.UnitTests.Catalog.FractionPricingCalculatorTests</c> e reutilizada
/// por <c>PreviewFractionPricingQueryHandler</c>. Vive em <c>Nexora.Application</c> (não em
/// <c>Nexora.Domain</c>) para espelhar o precedente já estabelecido por
/// <c>Nexora.Application.Catalog.Prices.Queries.ListVariantPricesByChannel.ChannelPriceResolver</c>
/// (US-014): outro cálculo puro, sem I/O, que também vive em Application em vez de Domain — a
/// justificativa lá (e aqui) é que o tipo de retorno com erro de negócio (<see cref="Result{T}"/>,
/// com o código estável de <see cref="ApiErrorCodes"/> já pronto para o controller mapear em HTTP)
/// só existe em Application; usar <see cref="Nexora.Domain.Common.DomainException"/> em vez disso
/// perderia a granularidade de código por cenário (ADR-021 exige um código por erro, não uma
/// mensagem genérica) e exigiria um catch específico em todo handler consumidor — que este
/// projeto não usa em nenhum outro fluxo (ver nota em <c>PreviewFractionPricingQueryHandler</c>).
/// <c>Nexora.Domain</c> continuaria uma escolha defensável (zero I/O, só <c>decimal</c>) caso o
/// projeto viesse a adotar <c>DomainException</c> tipada por código no futuro.
/// </summary>
public static class FractionPricingCalculator
{
    private const decimal RequiredWeightSum = 1.0m;
    private const int MinimumFractionCount = 2;

    /// <summary>
    /// Valida compatibilidade (tamanho, grupo de fração, soma de pesos) e calcula o preço final
    /// pela <paramref name="rule"/> informada. Não valida <c>AllowsFractions</c>/<c>MaxFractions</c>
    /// do produto — isso depende de carregar <c>Product</c> do banco e é responsabilidade do
    /// handler (que já precisa fazer essa consulta para resolver o preço vigente de cada variante).
    /// </summary>
    public static Result<FractionPricingCalculation> Calculate(IReadOnlyList<FractionPricingLine> fractions, FractionPriceRule rule)
    {
        if (fractions is null || fractions.Count < MinimumFractionCount)
        {
            return Result<FractionPricingCalculation>.Failure(
                "Um item meio a meio precisa de ao menos duas frações.",
                ApiErrorCodes.FractionMinimumNotMet);
        }

        if (fractions.Select(f => f.VariantId).Distinct().Count() != fractions.Count)
        {
            return Result<FractionPricingCalculation>.Failure(
                "O mesmo sabor não pode ser informado mais de uma vez.",
                ApiErrorCodes.FractionMinimumNotMet);
        }

        var sizes = fractions.Select(f => f.SizeCode ?? string.Empty).Distinct().ToArray();
        if (sizes.Length > 1)
        {
            return Result<FractionPricingCalculation>.Failure(
                "As frações devem ter o mesmo tamanho.",
                ApiErrorCodes.FractionSizeMismatch,
                new Dictionary<string, string[]> { ["sizes"] = fractions.Select(f => f.SizeCode ?? string.Empty).ToArray() });
        }

        var groups = fractions.Select(f => f.FractionGroup).ToArray();
        if (groups.Any(string.IsNullOrWhiteSpace) || groups.Distinct().Count() > 1)
        {
            return Result<FractionPricingCalculation>.Failure(
                "As frações pertencem a grupos de fracionamento diferentes.",
                ApiErrorCodes.FractionGroupMismatch,
                new Dictionary<string, string[]> { ["groups"] = groups.Select(g => g ?? string.Empty).ToArray() });
        }

        var weightSum = fractions.Sum(f => f.Weight);
        if (weightSum != RequiredWeightSum)
        {
            return Result<FractionPricingCalculation>.Failure(
                $"A soma dos pesos das frações deve ser exatamente 1,0 (recebido {weightSum}).",
                ApiErrorCodes.FractionWeightSumInvalid);
        }

        var unitPrice = rule switch
        {
            FractionPriceRule.Highest => fractions.Max(f => f.UnitPrice),
            // Arredondamento half-up (ADR-017), mesmo padrão de
            // BulkPriceAdjustmentCalculator.Apply — a US deixa em aberto a política de
            // arredondamento da regra AVERAGE com valores ímpares; decisão tomada aqui é
            // MidpointRounding.AwayFromZero (half-up) para consistência com o resto do sistema.
            FractionPriceRule.Average => RoundHalfUp(fractions.Average(f => f.UnitPrice)),
            FractionPriceRule.Proportional => RoundHalfUp(fractions.Sum(f => f.Weight * f.UnitPrice)),
            _ => throw new ArgumentOutOfRangeException(nameof(rule), rule, "Regra de precificação de fração desconhecida."),
        };

        return Result<FractionPricingCalculation>.Success(new FractionPricingCalculation(unitPrice, rule));
    }

    private static decimal RoundHalfUp(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
