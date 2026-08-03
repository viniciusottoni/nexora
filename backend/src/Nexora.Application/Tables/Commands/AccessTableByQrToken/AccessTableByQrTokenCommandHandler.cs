using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Tables.Sessions;
using Nexora.Contracts.Operation;
using Nexora.Domain.Operation;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tables.Commands.AccessTableByQrToken;

/// <summary>
/// Cenários Gherkin da US-021: "Acesso sem instalação", "Sessão de mesa já aberta", "Primeira
/// leitura da mesa", "Token inválido ou rotacionado" e "Retorno após fechar o navegador" (a mesma
/// sessão ativa é sempre devolvida — nenhum estado de "já visitei esta mesa" é necessário no
/// cliente, só o <c>qr_token</c> impresso).
/// </summary>
internal sealed class AccessTableByQrTokenCommandHandler : IRequestHandler<AccessTableByQrTokenCommand, Result<PublicTableAccessResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly IEventOriginProvider _eventOrigin;
    private readonly ITokenIssuer _tokenIssuer;

    public AccessTableByQrTokenCommandHandler(IApplicationDbContext db, IEventOriginProvider eventOrigin, ITokenIssuer tokenIssuer)
    {
        _db = db;
        _eventOrigin = eventOrigin;
        _tokenIssuer = tokenIssuer;
    }

    public async Task<Result<PublicTableAccessResponse>> Handle(AccessTableByQrTokenCommand request, CancellationToken cancellationToken)
    {
        var token = request.QrToken.Trim();

        // RN-015 + cenário "Token inválido ou rotacionado": a MESMA mensagem genérica cobre token
        // inexistente, rotacionado ou de mesa inativa/excluída — nunca revela qual dos três casos
        // aconteceu (isso já vazaria informação sobre a existência de outra mesa/token).
        var table = string.IsNullOrWhiteSpace(token)
            ? null
            : await _db.DiningTables
                .Include(t => t.Area)
                .SingleOrDefaultAsync(t => t.QrToken == token && t.DeletedAt == null && t.IsActive, cancellationToken);

        if (table is null)
        {
            return Result<PublicTableAccessResponse>.Failure(
                "Não conseguimos reconhecer esta mesa. Chame o garçom para continuar.",
                ApiErrorCodes.InvalidTableToken);
        }

        var existing = await _db.TableSessions.SingleOrDefaultAsync(
            s => s.TableId == table.Id && s.Status != Domain.Operation.TableSessionStatus.Closed, cancellationToken);

        TableSession session;
        if (existing is not null)
        {
            // Cenário "Sessão de mesa já aberta"/"Retorno após fechar o navegador": entra na
            // sessão existente, sem abrir uma nova nem emitir EVT-020 de novo.
            session = existing;
        }
        else
        {
            // Cenário "Primeira leitura da mesa": abre com source=QR, reaproveitando o mesmo
            // núcleo de abertura da US-022.
            var opened = await TableSessionOpener.OpenAsync(
                _db,
                _eventOrigin,
                table,
                requestedGuestCount: null,
                waiterId: null,
                openedBy: null,
                source: "QR",
                occurredAt: null,
                cancellationToken);

            if (opened.IsFailure)
            {
                return Result<PublicTableAccessResponse>.Failure(opened.Error!, opened.Code, opened.Errors);
            }

            session = opened.Value!;
        }

        var sessionToken = await _tokenIssuer.IssueTableSessionTokenAsync(
            session.Id, table.TenantId, table.Id, AuthTokenTtlSeconds.TableSession, cancellationToken);

        var response = new PublicTableAccessResponse(
            new PublicTableInfoResponse(table.Id, table.Label, table.Area?.Name ?? string.Empty),
            TableSessionMapper.Map(session, table),
            sessionToken);

        return Result<PublicTableAccessResponse>.Success(response);
    }
}
