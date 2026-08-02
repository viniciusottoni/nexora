using Nexora.Api.Edge.Infrastructure;
using Nexora.Application.Orders.Commands.AddOrderItem;
using Nexora.Application.Orders.Commands.AdvanceOrderItemStatus;
using Nexora.Application.Orders.Commands.RepeatOrderItem;
using Nexora.Contracts.Errors;
using Nexora.Contracts.Operation;
using Nexora.Shared.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Edge.Controllers;

/// <summary>
/// Lançamento, repetição e avanço de status de itens de pedido — capacidade mínima construída
/// para preencher a lacuna real de US-030 (ver docstring de
/// <c>Nexora.Application.Orders.Commands.AddOrderItem.AddOrderItemCommandHandler</c>) e o núcleo
/// de US-028 (Repetir item com um toque). Vive no edge: autoridade do dado de pedido/comanda é
/// local (mesmo motivo de <see cref="TableSessionsController"/>).
/// </summary>
[ApiController]
public sealed class OrderItemsController : ControllerBase
{
    private readonly ISender _sender;

    public OrderItemsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Lançamento mínimo de item na comanda — usado pelo garçom/POS e pelos testes/repetição desta
    /// wave para gerar dados reais de consumo (US-024 depende de "quatro itens lançados" para o
    /// cenário "Visualização do consumo" ter algo de verdade para listar).
    /// </summary>
    [HttpPost("v1/sessions/{sessionId:guid}/items")]
    [Authorize(Policy = "OrderAddItem")]
    [ProducesResponseType(typeof(OrderItemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Add(
        [FromRoute] Guid sessionId,
        [FromBody] AddOrderItemRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new AddOrderItemCommand(
                sessionId,
                request.VariantId,
                request.Quantity,
                request.Notes,
                request.Modifiers?.Select(m => new AddOrderItemModifierInput(m.ModifierId, m.Quantity)).ToList(),
                request.Fractions?.Select(f => new AddOrderItemFractionInput(f.VariantId, f.Weight)).ToList()),
            cancellationToken);

        if (result.IsSuccess)
        {
            return StatusCode(StatusCodes.Status201Created, result.Value);
        }

        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// US-028 §7. Disponível para cliente (esquema <c>TableSession</c>, item da PRÓPRIA comanda —
    /// RN-015, ver docstring de <see cref="RepeatOrderItemCommand"/>) e para garçom/POS (esquema
    /// padrão, permissão <c>order:add_item</c>) — os dois esquemas são aceitos nesta MESMA rota
    /// porque o contrato de API da história é único; a distinção de quem chamou é resolvida aqui
    /// (onde o <see cref="HttpContext"/> naturalmente vive), nunca dentro do handler de Application
    /// (ADR-039 proíbe Application de depender de ASP.NET Core).
    /// </summary>
    [HttpPost("v1/orders/{orderId:guid}/items/{itemId:guid}/repeat")]
    [Authorize(AuthenticationSchemes = "Bearer,TableSession")]
    [ProducesResponseType(typeof(RepeatOrderItemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Repeat(
        [FromRoute] Guid orderId,
        [FromRoute] Guid itemId,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        Guid? requestingSessionId = null;

        if (User.FindFirst("tokenUse")?.Value == "table_session")
        {
            // Cliente do salão: a claim "ses" do PRÓPRIO token vira o filtro de posse no handler —
            // nunca um valor informado pela requisição.
            var sessionClaim = User.FindFirst("ses")?.Value;
            if (!Guid.TryParse(sessionClaim, out var sessionId))
            {
                return NotFound();
            }

            requestingSessionId = sessionId;
        }
        else
        {
            var permissions = User.FindAll(PermissionAuthorization.PermissionClaimType).Select(c => c.Value).ToArray();
            if (!PermissionAuthorization.HasPermission(permissions, "order:add_item"))
            {
                return Forbid();
            }
        }

        var result = await _sender.Send(new RepeatOrderItemCommand(orderId, itemId, requestingSessionId), cancellationToken);

        if (result.IsSuccess)
        {
            return StatusCode(StatusCodes.Status201Created, result.Value);
        }

        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// Avança um item um passo na fila de produção — ver docstring completa do gap de escopo em
    /// <c>AdvanceOrderItemStatusCommandHandler</c> (não é o KDS, é o mínimo para provar a entrega
    /// em tempo real de ponta a ponta da US-024).
    /// </summary>
    [HttpPost("v1/orders/{orderId:guid}/items/{itemId:guid}/advance")]
    [Authorize(Policy = "KdsAdvance")]
    [ProducesResponseType(typeof(OrderItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Advance(
        [FromRoute] Guid orderId,
        [FromRoute] Guid itemId,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new AdvanceOrderItemStatusCommand(orderId, itemId), cancellationToken);
        return result.ToActionResult(HttpContext);
    }
}
