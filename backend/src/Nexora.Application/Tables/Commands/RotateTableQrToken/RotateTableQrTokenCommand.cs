using Nexora.Application.Abstractions.Messaging;

namespace Nexora.Application.Tables.Commands.RotateTableQrToken;

/// <summary>
/// Cenário Gherkin "Rotação de token": gera um novo <c>qr_token</c> para a mesa — o anterior
/// deixa de resolver imediatamente. Porta de <c>POST /v1/tables/{id}/rotate-token</c>. Sem
/// retorno de dado: o token nunca sai do servidor em JSON puro (só embutido no PNG do PDF de
/// exportação) — o gestor precisa reexportar o PDF da mesa para obter o QR Code novo.
/// </summary>
public sealed record RotateTableQrTokenCommand(Guid TableId) : ICommand;
