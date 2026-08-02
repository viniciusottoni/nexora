using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Catalog.Prices;
using Nexora.Application.Catalog.Prices.Queries.ListVariantPricesByChannel;
using Nexora.Contracts.Catalog;
using Nexora.Domain.Catalog;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Catalog.FractionPricing.Queries.PreviewFractionPricing;

/// <summary>
/// Handler de <see cref="PreviewFractionPricingQuery"/> (US-013) — carrega as variantes/produtos
/// envolvidos, resolve o preço vigente de cada uma no canal consultado (reaproveitando
/// <see cref="ChannelPriceResolver"/>, a mesma lógica de "preço efetivo por canal" da US-014, em
/// vez de duplicá-la — ela já resolve corretamente a herança do preço base quando o canal não tem
/// preço próprio), resolve a regra de precificação vigente do tenant
/// (<see cref="FractionPriceRuleResolver"/>) e delega o cálculo/validação de negócio a
/// <see cref="FractionPricingCalculator"/>.
/// </summary>
/// <remarks>
/// Autenticado (<c>[Authorize(Policy = "ProductRead")]</c> no controller), igual a
/// <c>ListVariantPricesByChannelQueryHandler</c> — não é o endpoint público
/// (<c>[AllowAnonymous]</c>) de <c>PublicMenuController</c>. Cogitou-se espelhar o mecanismo de
/// resolução de tenant por <c>Tenant.Domain</c> (US-010 §7) para que o cliente do salão
/// (persona P1, sem login) pudesse chamar este preview diretamente do app de cardápio da mesa,
/// mas essa via foi descartada aqui: <c>Tenant.Domain</c> não tem NENHUM método de escrita em todo
/// o Domain hoje (nem <c>Tenant.Create</c> nem qualquer outro), e não há um único teste de
/// integração no repositório que efetivamente popule esse campo e exerça a resolução por host —
/// construir a parte mais testada desta história (o cálculo) em cima de um mecanismo já hoje
/// inerte importaria esse gap para US-013. Ficou como consumidor autenticado (mesma família de
/// <c>ProductRead</c>/<c>ProductWrite</c> de catálogo), plugável ao app de cardápio do cliente
/// assim que a resolução pública de tenant for de fato implementada (fora do escopo desta tarefa).
/// </remarks>
internal sealed class PreviewFractionPricingQueryHandler : IRequestHandler<PreviewFractionPricingQuery, Result<PreviewFractionPricingResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public PreviewFractionPricingQueryHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PreviewFractionPricingResponse>> Handle(PreviewFractionPricingQuery request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result<PreviewFractionPricingResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado ao seu usuário.",
                ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;

        // Canal ausente/ inválido cai em DineIn — mesmo padrão defensivo de GetPublicMenuQueryHandler
        // (US-010): um parâmetro opcional mal formado não derruba o preview inteiro.
        if (!PricingChannelParser.TryParse(request.Channel, out var channel))
        {
            channel = Channel.DineIn;
        }

        var variantIds = request.Fractions.Select(f => f.VariantId).Distinct().ToList();

        var variants = await _db.ProductVariants
            .AsNoTracking()
            .Include(v => v.Product)
            .Where(v =>
                v.TenantId == tenantId
                && v.DeletedAt == null
                && v.IsActive
                && v.Product.IsActive
                && v.Product.IsAvailable
                && variantIds.Contains(v.Id))
            .ToListAsync(cancellationToken);

        if (variants.Count != variantIds.Count)
        {
            return Result<PreviewFractionPricingResponse>.Failure(
                "Uma ou mais variantes não foram encontradas.",
                ApiErrorCodes.FractionVariantNotFound);
        }

        var variantById = variants.ToDictionary(v => v.Id);

        var notAllowed = variants.FirstOrDefault(v => !v.Product.AllowsFractions);
        if (notAllowed is not null)
        {
            return Result<PreviewFractionPricingResponse>.Failure(
                $"O produto \"{notAllowed.Product.Name}\" não permite fracionamento.",
                ApiErrorCodes.FractionNotAllowed);
        }

        // O limite efetivo é o menor MaxFractions entre os produtos envolvidos — MaxFractions
        // vive no Product (não na variante, apesar do texto da US-013 citar
        // "product_variant.fraction_group": o modelo real implementado é a fonte da verdade,
        // ver contexto da tarefa), e um meio a meio combina variantes de produtos diferentes
        // (ex.: produto "Mussarela" + produto "Calabresa").
        var maxFractions = variants.Min(v => v.Product.MaxFractions);
        if (request.Fractions.Count > maxFractions)
        {
            return Result<PreviewFractionPricingResponse>.Failure(
                $"Este item permite no máximo {maxFractions} sabor(es), mas {request.Fractions.Count} foram informados.",
                ApiErrorCodes.FractionMaxExceeded);
        }

        var currentPrices = await _db.Prices
            .AsNoTracking()
            .Where(p => variantIds.Contains(p.VariantId) && p.ValidTo == null)
            .ToListAsync(cancellationToken);

        var pricesByVariant = currentPrices
            .GroupBy(p => p.VariantId)
            .ToDictionary(g => g.Key, g => (IReadOnlyCollection<Price>)g.ToList());

        var lines = new List<FractionPricingLine>(request.Fractions.Count);
        foreach (var fraction in request.Fractions)
        {
            var variant = variantById[fraction.VariantId];
            pricesByVariant.TryGetValue(variant.Id, out var variantPrices);
            var resolved = ChannelPriceResolver.Resolve(channel, variantPrices ?? Array.Empty<Price>());

            if (resolved.Amount is null)
            {
                return Result<PreviewFractionPricingResponse>.Failure(
                    $"A variante \"{variant.Name}\" não tem preço vigente no canal {channel}.",
                    ApiErrorCodes.FractionPriceNotFound);
            }

            lines.Add(new FractionPricingLine(variant.Id, fraction.Weight, resolved.Amount.Value, variant.SizeCode, variant.Product.FractionGroup));
        }

        var tenantConfig = await _db.TenantConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(tc => tc.TenantId == tenantId, cancellationToken);
        var rule = FractionPriceRuleResolver.Resolve(tenantConfig?.Operation);

        var calculation = FractionPricingCalculator.Calculate(lines, rule);
        if (calculation.IsFailure)
        {
            return Result<PreviewFractionPricingResponse>.Failure(calculation.Error!, calculation.Code, calculation.Errors);
        }

        var description = BuildDescription(request, variantById);

        var responseLines = lines
            .Select(l => new FractionPricingLineResponse(l.VariantId, l.Weight, l.UnitPrice))
            .ToList();

        var response = new PreviewFractionPricingResponse(
            calculation.Value!.UnitPrice,
            calculation.Value.Rule.ToString().ToUpperInvariant(),
            description,
            responseLines);

        return Result<PreviewFractionPricingResponse>.Success(response);
    }

    /// <summary>
    /// Monta a descrição composta (US-013 §4/§10, cenário "Exibição no KDS": <c>"Pizza G ·
    /// Mussarela / Calabresa"</c>). Desvio deliberado do exemplo literal do documento: o prefixo
    /// fixo <c>"Pizza"</c> não é reproduzido aqui porque não existe, no modelo de dados real desta
    /// solution, nenhum campo que nomeie genericamente "a família do produto" independente do
    /// nome de cada sabor — inventar esse texto fixo violaria o ADR-013 (nenhuma diferença de
    /// negócio vira literal de código; um estabelecimento que vende hambúrguer meio a meio não
    /// deveria ver "Pizza" na descrição). A descrição sai como <c>"{SizeCode} · {sabor 1} / {sabor
    /// 2}"</c>, preservando a ordem em que o cliente escolheu os sabores.
    /// </summary>
    private static string BuildDescription(
        PreviewFractionPricingQuery request,
        IReadOnlyDictionary<Guid, ProductVariant> variantById)
    {
        var sizeCode = variantById[request.Fractions[0].VariantId].SizeCode;
        var flavorNames = request.Fractions.Select(f => variantById[f.VariantId].Product.Name);
        var flavors = string.Join(" / ", flavorNames);

        return string.IsNullOrWhiteSpace(sizeCode) ? flavors : $"{sizeCode} · {flavors}";
    }
}
