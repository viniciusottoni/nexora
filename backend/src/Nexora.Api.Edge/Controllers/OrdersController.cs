using Nexora.Api.Edge.Infrastructure;
using Nexora.Api.Edge.Infrastructure.Auth;
using Nexora.Application.Orders.Commands.AddItemToOrder;
using Nexora.Application.Orders.Commands.AddOrderItem;
using Nexora.Application.Orders.Commands.CancelOrder;
using Nexora.Application.Orders.Commands.CreateOrder;
using Nexora.Application.Orders.Queries.GetOrder;
using Nexora.Contracts.Errors;
using Nexora.Contracts.Operation;
using Nexora.Shared.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Edge.Controllers;

/// <summary>
/// US-030 (Criar pedido com itens, modificadores e frações) — <c>POST /v1/orders</c> (garçom/POS),
/// <c>POST /v1/public/orders</c> (cliente na mesa via QR), <c>GET /v1/orders/{id}</c> e
/// <c>POST /v1/orders/{id}/items</c> (acréscimo a pedido já confirmado). Vive no edge: autoridade do
/// dado de pedido é local (doc. 02 §2.1 "pedido é criado no local e apenas lido na nuvem"), mesmo
/// motivo de <see cref="OrderItemsController"/>/<see cref="TableSessionsController"/>.
/// </summary>
[ApiController]
public sealed class OrdersController : ControllerBase
{
    private readonly ISender _sender;

    public OrdersController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// US-030 §7 — garçom/POS autenticado. <c>channel</c>/<c>sessionId</c> vêm do corpo (o garçom
    /// escolhe a mesa e o canal explicitamente); <c>X-Occurred-At</c> preserva o horário real de
    /// criação quando o pedido foi montado offline e só sincronizou depois (US-030 §9/ADR-034).
    /// </summary>
    [HttpPost("v1/orders")]
    [Authorize(Policy = "OrderCreate")]
    [ProducesResponseType(typeof(CreateOrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrderRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromHeader(Name = "X-Occurred-At")] DateTimeOffset? occurredAt,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreateOrderCommand(request.Channel, request.SessionId, MapItems(request.Items), occurredAt),
            cancellationToken);

        if (result.IsSuccess)
        {
            return StatusCode(StatusCodes.Status201Created, result.Value);
        }

        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// US-030 §7 — caminho público do cliente na mesa (QR). <c>channel</c> é sempre <c>DineIn</c> e
    /// <c>sessionId</c>/<c>tableId</c> vêm das claims <c>ses</c>/<c>tbl</c> do PRÓPRIO token
    /// (RN-015, mesmo padrão de <see cref="PublicTableController"/>) — nunca de um valor que o
    /// corpo da requisição poderia informar.
    /// </summary>
    [HttpPost("v1/public/orders")]
    [Authorize(Policy = "SessionScope")]
    [ProducesResponseType(typeof(CreateOrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreatePublic(
        [FromBody] CreatePublicOrderRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromHeader(Name = "X-Occurred-At")] DateTimeOffset? occurredAt,
        CancellationToken cancellationToken)
    {
        var sessionClaim = User.FindFirst("ses")?.Value;
        if (!Guid.TryParse(sessionClaim, out var sessionId))
        {
            return NotFound();
        }

        var result = await _sender.Send(
            new CreateOrderCommand("DineIn", sessionId, MapItems(request.Items), occurredAt),
            cancellationToken);

        if (result.IsSuccess)
        {
            return StatusCode(StatusCodes.Status201Created, result.Value);
        }

        return result.ToActionResult(HttpContext);
    }

    /// <summary>US-030 §7 — devolve o pedido com os itens (ADR-021 princípio 7).</summary>
    [HttpGet("v1/orders/{id:guid}")]
    [Authorize(Policy = "OrderRead")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetOrderQuery(id), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// US-030 §7, cenário "Acréscimo a pedido já confirmado" — aceita os DOIS esquemas de
    /// autenticação (cliente da mesa via QR, ou garçom/POS com a permissão <c>order:add_item</c>),
    /// mesmo padrão de <see cref="OrderItemsController.Repeat"/>: a claim de sessão do token de
    /// mesa vira o filtro de posse (RN-015) no handler, nunca um valor informado pela requisição.
    /// </summary>
    [HttpPost("v1/orders/{id:guid}/items")]
    [Authorize(AuthenticationSchemes = "Bearer,TableSession")]
    [ProducesResponseType(typeof(OrderItemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AddItem(
        [FromRoute] Guid id,
        [FromBody] CreateOrderItemRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromHeader(Name = "X-Occurred-At")] DateTimeOffset? occurredAt,
        CancellationToken cancellationToken)
    {
        Guid? requestingSessionId = null;

        if (User.FindFirst("tokenUse")?.Value == "table_session")
        {
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

        var result = await _sender.Send(
            new AddItemToOrderCommand(
                id,
                request.VariantId,
                request.Quantity,
                request.Notes,
                request.Modifiers?.Select(m => new AddOrderItemModifierInput(m.ModifierId, m.Quantity)).ToList(),
                request.Fractions?.Select(f => new AddOrderItemFractionInput(f.VariantId, f.Weight)).ToList(),
                occurredAt,
                requestingSessionId),
            cancellationToken);

        if (result.IsSuccess)
        {
            return StatusCode(StatusCodes.Status201Created, result.Value);
        }

        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// US-033 §7 — cancela o pedido inteiro (todos os itens ativos, na mesma operação). Mesma
    /// convenção de <c>X-Authorization-Token</c> OPCIONAL de
    /// <see cref="OrderItemsController.Cancel"/> — só exigido quando algum item já foi iniciado.
    /// </summary>
    [HttpPost("v1/orders/{id:guid}/cancel")]
    [Authorize(Policy = "OrderCancelItem")]
    [ProducesResponseType(typeof(CancelOrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(
        [FromRoute] Guid id,
        [FromBody] CancelOrderRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromHeader(Name = RequiresAuthorizationTokenAttribute.HeaderName)] string? authorizationToken,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CancelOrderCommand(id, request.Reason, request.Notes, authorizationToken),
            cancellationToken);

        return result.ToActionResult(HttpContext);
    }

    private static IReadOnlyList<CreateOrderItemInput> MapItems(IReadOnlyList<CreateOrderItemRequest> items) =>
        items.Select(i => new CreateOrderItemInput(
            i.VariantId,
            i.Quantity,
            i.Notes,
            i.Modifiers?.Select(m => new AddOrderItemModifierInput(m.ModifierId, m.Quantity)).ToList(),
            i.Fractions?.Select(f => new AddOrderItemFractionInput(f.VariantId, f.Weight)).ToList())).ToList();
}
