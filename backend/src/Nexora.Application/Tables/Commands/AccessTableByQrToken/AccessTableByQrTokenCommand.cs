using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Operation;

namespace Nexora.Application.Tables.Commands.AccessTableByQrToken;

/// <summary>
/// Resolve a mesa pelo <c>qr_token</c> e abre a sessão automaticamente quando ainda não há uma
/// ativa (US-021 §3.1: "Abertura automática de sessão de mesa se ainda não houver uma (com a
/// US-022)")). Porta de <c>GET /v1/public/table/{qrToken}</c>.
/// </summary>
/// <remarks>
/// [DECISÃO] Modelado como <see cref="ICommand{TResponse}"/>, não <see cref="IQuery{TResponse}"/>,
/// apesar do verbo HTTP ser GET: quando a mesa está livre, esta operação MUTA estado (abre sessão,
/// ocupa a mesa, grava evento) — precisa do <see cref="Abstractions.Behaviors.TransactionBehavior"/>
/// (que só envolve <c>ICommand</c>) para gravar estado+evento na mesma transação (ADR-006). É
/// idempotente por natureza (não por <c>Idempotency-Key</c>, dispensável aqui — GET nunca exige
/// esse header): ler o mesmo QR várias vezes sempre devolve a MESMA sessão ativa, nunca abre uma
/// segunda (ver <see cref="Sessions.TableSessionOpener"/>).
/// </remarks>
public sealed record AccessTableByQrTokenCommand(string QrToken) : ICommand<PublicTableAccessResponse>;
