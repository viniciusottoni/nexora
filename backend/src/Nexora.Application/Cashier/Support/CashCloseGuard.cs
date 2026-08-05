using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Tables.Sessions;
using Nexora.Contracts.Cashier;
using Nexora.Domain.Operation;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Cashier.Support;

/// <summary>
/// RN-018 (US-055 §5): "caixa não pode ser fechado com mesa aberta, salvo autorização registrada".
/// Núcleo único da checagem, no mesmo espírito de <c>Tables.Support.PendingItemsClosePolicy</c> —
/// resolve as mesas ainda abertas NA LOJA do caixa (todo <see cref="TableSessionStatus"/> diferente
/// de <see cref="TableSessionStatus.Closed"/>), exige <c>X-Authorization-Token</c> para a ação
/// <see cref="CloseWithOpenTablesAction"/> quando existe pelo menos uma, e — quando autorizado —
/// grava o <see cref="AuditLog"/> exigido pela US-055 §8 (<c>action=CLOSE_CASH_WITH_OPEN_TABLES</c>).
/// </summary>
/// <remarks>
/// [DECISÃO DOCUMENTADA] A US-055 §7 mostra o header <c>X-Authorization-Token</c> só no cenário de
/// mesa aberta (não no de divergência, que só exige o campo <c>justification</c> no corpo — ver
/// <c>CashPolicy</c>). Reaproveita a ação sensível <c>CLOSE_DIVERGENT_CASH</c> já catalogada em
/// <c>SensitiveActionCatalog</c>/<c>PermissionCatalog</c> (permissão <c>cash:close_divergent</c>) em
/// vez de criar uma ação nova: é a mesma pessoa (perfil superior) que autoriza os dois tipos de
/// "fechamento de caixa fora do fluxo normal", e nada na spec sugere uma permissão distinta para
/// mesa aberta especificamente.
/// </remarks>
public static class CashCloseGuard
{
    public const string CloseWithOpenTablesAction = "CLOSE_DIVERGENT_CASH";

    /// <summary>Chave reservada em <c>Result.Errors</c> (convenção de <c>ResultExtensions.ExtractMeta</c>) — JSON serializado da lista de mesas abertas.</summary>
    public const string MetaErrorsKey = "openTablesJson";

    /// <summary>
    /// <c>JsonSerializer.Serialize&lt;T&gt;()</c> sem opções explícitas preserva o nome do membro C#
    /// (PascalCase) — o contrato de fio da US-055 §7 (<c>meta.openSessions: [{ table, total }]</c>)
    /// exige camelCase, então a serialização do meta (consumida depois por <c>ResultExtensions</c>
    /// como <c>JsonElement</c> cru) precisa desta policy explícita. <see cref="OpenTableSessionInfo.Total"/>
    /// continua string (ADR-017, <c>MoneyJsonConverter</c> por atributo de propriedade — atributo de
    /// conversor sempre vence, independente da policy de nomes do options passado aqui).
    /// </summary>
    private static readonly JsonSerializerOptions MetaJsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static async Task<Result<CashCloseGuardOutcome>> EnforceAsync(
        IApplicationDbContext db,
        IAuthorizationTokenValidator authorizationValidator,
        Guid tenantId,
        Guid storeId,
        Guid cashSessionId,
        string? authorizationToken,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var openSessions = await db.TableSessions
            .Include(s => s.Table)
            .AsNoTracking()
            .Where(s => s.StoreId == storeId && s.Status != TableSessionStatus.Closed)
            .ToListAsync(cancellationToken);

        if (openSessions.Count == 0)
        {
            return Result<CashCloseGuardOutcome>.Success(new CashCloseGuardOutcome(new List<OpenTableSessionInfo>(), null));
        }

        var openTables = new List<OpenTableSessionInfo>(openSessions.Count);
        foreach (var session in openSessions)
        {
            var total = await SessionTotalAsync(db, session.Id, cancellationToken);
            openTables.Add(new OpenTableSessionInfo(session.Table.Label, total));
        }

        var grant = await authorizationValidator.ValidateAsync(authorizationToken, CloseWithOpenTablesAction, cancellationToken);
        if (grant.IsFailure)
        {
            return Result<CashCloseGuardOutcome>.Failure(
                "Existem mesas ainda abertas — feche-as ou autorize o fechamento do caixa mesmo assim.",
                ApiErrorCodes.OpenTables,
                new Dictionary<string, string[]> { [MetaErrorsKey] = new[] { JsonSerializer.Serialize(openTables, MetaJsonOptions) } });
        }

        db.AuditLogs.Add(AuditLog.Create(
            tenantId,
            action: "CLOSE_CASH_WITH_OPEN_TABLES",
            entity: "cash_session",
            occurredAt: now,
            storeId: storeId,
            actorId: grant.Value!.ActorId,
            authorizedBy: grant.Value.AuthorizedBy,
            entityId: cashSessionId,
            after: JsonSerializer.Serialize(new { openTables })));

        return Result<CashCloseGuardOutcome>.Success(new CashCloseGuardOutcome(openTables, grant.Value.AuthorizedBy));
    }

    private static async Task<decimal> SessionTotalAsync(IApplicationDbContext db, Guid sessionId, CancellationToken cancellationToken)
    {
        var items = await BillQueryCoordinator.LoadItemsAsync(db, sessionId, cancellationToken);
        return items.Where(i => i.Status != OrderItemStatus.Cancelled).Sum(i => i.TotalPrice);
    }
}

/// <summary>Resultado de <see cref="CashCloseGuard.EnforceAsync"/> — mesas abertas encontradas (vazio quando nenhuma) e quem autorizou o contorno (nulo quando não havia mesa aberta, portanto nada a autorizar).</summary>
public sealed record CashCloseGuardOutcome(IReadOnlyList<OpenTableSessionInfo> OpenTables, Guid? AuthorizedBy);
