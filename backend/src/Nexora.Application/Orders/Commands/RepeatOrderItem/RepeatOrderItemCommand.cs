using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Operation;

namespace Nexora.Application.Orders.Commands.RepeatOrderItem;

/// <summary>
/// Porta de <c>POST /v1/orders/{orderId}/items/{itemId}/repeat</c> (US-028 §7) — "Disponível para
/// cliente e para garçom" (US-028 §3.1). <paramref name="RequestingSessionId"/> só é preenchido
/// quando quem chama é o cliente autenticado pelo esquema <c>TableSession</c> (a claim <c>ses</c>
/// do próprio token, resolvida pelo controller — nunca informada pelo corpo da requisição): o
/// handler recusa com <see cref="Nexora.Shared.Errors.ApiErrorCodes.OrderNotFound"/> (404, nunca
/// 403 — ADR-021/RN-015) se o pedido do item não pertencer à sessão do token, para que o cliente
/// da mesa 12 nunca consiga repetir um item da mesa 13 mesmo sabendo o id do pedido. Quando quem
/// chama é a equipe (garçom/POS, esquema padrão), o controller não preenche este campo — a
/// permissão <c>order:add_item</c> já foi verificada na policy do endpoint.
/// </summary>
public sealed record RepeatOrderItemCommand(Guid OrderId, Guid ItemId, Guid? RequestingSessionId = null) : ICommand<RepeatOrderItemResponse>;
