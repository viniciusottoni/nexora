using Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Catalog.FractionPricing.Queries.PreviewFractionPricing;
using Nexora.Contracts.Catalog;
using Nexora.Contracts.Errors;
using Nexora.Contracts.Http;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Cloud.Controllers;

/// <summary>
/// Precificação de fração — meio a meio (US-013). Sub-recurso de catálogo, mesma família de
/// permissão de <see cref="ProductVariantsController"/>/<see cref="PriceAdjustmentsController"/>
/// (<c>catalog:read</c>). Só expõe o preview de cálculo (§7 do documento da US sugere um contrato
/// de <c>POST /v1/public/orders</c> completo, mas criar pedido — sessão de mesa, canal, ciclo de
/// vida — é escopo de uma epic de Atendimento/Pedidos que ainda não existe nesta solution: não há
/// <c>OrderController</c> nem qualquer camada de Application para <c>Nexora.Domain.Operation</c>
/// hoje). Este controller não persiste <c>order</c>/<c>order_item</c> nenhum — ver relatório da
/// tarefa para a decisão de escopo completa.
/// </summary>
[ApiController]
[Authorize]
public sealed class FractionPricingController : ControllerBase
{
    private readonly ISender _sender;

    public FractionPricingController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Calcula o preço final de um item meio a meio (2+ frações) pela regra vigente do tenant e
    /// monta a descrição composta — sem persistir nada (US-013 §4, "preço final visível antes de
    /// confirmar"). Isento de <c>Idempotency-Key</c> (<see cref="IdempotencyExemptAttribute"/>):
    /// é um cálculo puro, sem efeito colateral duplicável — chamar duas vezes com o mesmo corpo
    /// não corrompe nenhum estado nem duplica um recurso de negócio (mesmo raciocínio já usado
    /// para login/refresh, ver docstring do atributo).
    /// </summary>
    [HttpPost("v1/catalog/fraction-pricing/preview")]
    [Authorize(Policy = "ProductRead")]
    [IdempotencyExempt]
    [ProducesResponseType(typeof(PreviewFractionPricingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Preview([FromBody] PreviewFractionPricingRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new PreviewFractionPricingQuery(request.Fractions, request.Channel), cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
