using Nexora.Api.Edge.Infrastructure;
using Nexora.Application.Tables.Commands.AcknowledgeWaiterCall;
using Nexora.Application.Tables.Commands.AssignBillItems;
using Nexora.Application.Tables.Commands.OpenTableSession;
using Nexora.Application.Tables.Commands.RegisterPartialPayment;
using Nexora.Application.Tables.Commands.RequestBill;
using Nexora.Application.Tables.Commands.UpdateTableSession;
using Nexora.Application.Tables.Commands.WaiveServiceFee;
using Nexora.Application.Tables.Queries.GetBill;
using Nexora.Application.Tables.Queries.GetTableSession;
using Nexora.Contracts.Errors;
using Nexora.Contracts.Operation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Edge.Controllers;

/// <summary>
/// Abertura, alteração e consulta de sessão de mesa pelo garçom (US-022) — autoridade do dado é o
/// edge (RF-SAL-04, cabeçalho "Autoridade do dado: Local" da US), diferente do CRUD de
/// <c>dining_table</c>/<c>area</c> em si (US-020), que é autoridade da nuvem. Por isso este
/// controller vive em <c>Nexora.Api.Edge</c>, não em <c>Nexora.Api.Cloud</c> — o gêmeo do lado da
/// nuvem não existe porque a nuvem nunca abre mesa.
/// </summary>
[ApiController]
[Authorize]
public sealed class TableSessionsController : ControllerBase
{
    private readonly ISender _sender;

    public TableSessionsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Cenários Gherkin "Abertura pelo garçom", "Mesa já ocupada" e "Duplo toque do garçom"
    /// (US-022 §4). <c>X-Occurred-At</c> preserva o horário real da abertura mesmo com sync
    /// atrasado (RN-020, US-022 §9) — opcional: sem o header, usa o relógio do servidor local (a
    /// abertura de mesa nunca é recusada por falta desse cabeçalho, RF-OFF-01). O corpo da
    /// resposta em 409 traz <c>meta.sessionId</c> para o app do garçom redirecionar direto à
    /// sessão existente, sem uma segunda chamada.
    /// </summary>
    [HttpPost("v1/tables/{id:guid}/sessions")]
    [Authorize(Policy = "TableOpen")]
    [ProducesResponseType(typeof(TableSessionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Open(
        [FromRoute] Guid id,
        [FromBody] OpenTableSessionRequest request,
        [FromHeader(Name = "X-Occurred-At")] DateTimeOffset? occurredAt,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new OpenTableSessionCommand(id, request.GuestCount, occurredAt), cancellationToken);
        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value);
        }

        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// Cenário Gherkin "Troca de garçom responsável" (US-022 §4) e alteração de contagem de
    /// pessoas — inclusive a confirmação da contagem pendente de uma sessão aberta por QR
    /// (US-021).
    /// </summary>
    [HttpPatch("v1/sessions/{id:guid}")]
    [Authorize(Policy = "TableOpen")]
    [ProducesResponseType(typeof(TableSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateTableSessionRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new UpdateTableSessionCommand(id, request.GuestCount, request.WaiterId), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>Consulta de sessão pelo garçom/app do salão.</summary>
    [HttpGet("v1/sessions/{id:guid}")]
    [Authorize(Policy = "TableRead")]
    [ProducesResponseType(typeof(TableSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetTableSessionQuery(id), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// US-025 §7 — o garçom confirma que atendeu a chamada da mesa, resolvendo o alerta pendente.
    /// [DECISÃO] Reaproveita a policy <c>TableOpen</c> (não uma <c>TableAcknowledge</c> nova): quem
    /// já pode abrir/gerenciar a sessão da mesa (garçom responsável ou qualquer um com
    /// <c>table:manage</c>) pode confirmar que atendeu à chamada dela — não há um perfil distinto
    /// "confirma chamada mas não abre mesa" no catálogo de papéis desta wave, e criar uma policy só
    /// para este verbo específico adicionaria uma permissão nova ao catálogo fechado
    /// (<c>PermissionCatalog</c>) sem um caso de uso real que a distinga de <c>table:open</c>.
    /// </summary>
    [HttpPost("v1/tables/{id:guid}/acknowledge-call")]
    [Authorize(Policy = "TableOpen")]
    [ProducesResponseType(typeof(AcknowledgeWaiterCallResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AcknowledgeCall(
        [FromRoute] Guid id,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new AcknowledgeWaiterCallCommand(id), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// US-026 §7, cenário "Solicitação pelo garçom" — mesma policy <c>TableOpen</c> de
    /// <see cref="AcknowledgeCall"/>, mesmo raciocínio (quem administra a sessão da mesa também
    /// pode marcá-la como "conta solicitada" direto do mapa).
    /// </summary>
    [HttpPost("v1/sessions/{id:guid}/request-bill")]
    [Authorize(Policy = "TableOpen")]
    [ProducesResponseType(typeof(RequestBillResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RequestBill(
        [FromRoute] Guid id,
        [FromBody] RequestBillRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromHeader(Name = "X-Authorization-Token")] string? authorizationToken,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new RequestBillCommand(id, request.SplitMode, request.People, authorizationToken, request.Reason), cancellationToken);
        if (result.IsSuccess)
        {
            return StatusCode(StatusCodes.Status202Accepted, result.Value);
        }

        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// US-027 §7 — prévia da divisão da conta (staff). Os query params SOBREPÕEM a preferência
    /// registrada em <c>request-bill</c> (US-026) quando informados (US-027 §10: "trocar de modo na
    /// hora de ver a prévia"). <c>waived</c> é uma lista de pessoas separada por vírgula (ex. <c>"1,3"</c>).
    /// </summary>
    [HttpGet("v1/sessions/{id:guid}/bill")]
    [Authorize(Policy = "TableRead")]
    [ProducesResponseType(typeof(BillResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GetBill(
        [FromRoute] Guid id,
        [FromQuery] string? split,
        [FromQuery] short? people,
        [FromQuery] decimal? amount,
        [FromQuery] string? waived,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetBillQuery(id, split, people, amount, waived), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// US-027 §7, modo <c>BY_ITEM</c> — atribuição de itens por pessoa. Não persiste (ver docstring
    /// de <see cref="AssignBillItemsCommand"/>): o POS reenvia a atribuição completa a cada chamada.
    /// </summary>
    [HttpPost("v1/sessions/{id:guid}/bill/assign-items")]
    [Authorize(Policy = "BillManage")]
    [ProducesResponseType(typeof(BillResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AssignBillItems(
        [FromRoute] Guid id,
        [FromBody] AssignBillItemsRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var assignments = request.Assignments.Select(a => new BillItemAssignmentInput(a.Person, a.ItemIds)).ToList();
        var result = await _sender.Send(
            new AssignBillItemsCommand(id, assignments, request.ServiceFeeWaivedPersons), cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>US-027 §4, cenário "Retirada da taxa por uma das partes" — registrada e auditada (RN-010).</summary>
    [HttpPost("v1/sessions/{id:guid}/bill/waive-service-fee")]
    [Authorize(Policy = "BillManage")]
    [ProducesResponseType(typeof(BillResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> WaiveServiceFee(
        [FromRoute] Guid id,
        [FromBody] WaiveServiceFeeRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new WaiveServiceFeeCommand(id, request.People, request.Person, request.AlreadyWaivedPersons, request.Reason),
            cancellationToken);
        return result.ToActionResult(HttpContext);
    }

    /// <summary>US-027 §4, cenário "Divisão por valor" — pagamento parcial; a sessão permanece em <c>BILL_REQUESTED</c>.</summary>
    [HttpPost("v1/sessions/{id:guid}/bill/partial-payment")]
    [Authorize(Policy = "PaymentRegister")]
    [ProducesResponseType(typeof(PartialPaymentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RegisterPartialPayment(
        [FromRoute] Guid id,
        [FromBody] RegisterPartialPaymentRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromHeader(Name = "X-Authorization-Token")] string? authorizationToken,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new RegisterPartialPaymentCommand(id, request.Amount, request.Method, authorizationToken, request.Reason), cancellationToken);
        if (result.IsSuccess)
        {
            return StatusCode(StatusCodes.Status201Created, result.Value);
        }

        return result.ToActionResult(HttpContext);
    }
}
