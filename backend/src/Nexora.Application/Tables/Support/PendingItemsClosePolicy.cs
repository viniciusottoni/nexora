using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Orders.Support;
using Nexora.Application.Tables.Sessions;
using Nexora.Contracts.Operation;
using Nexora.Domain.Operation;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Tables.Support;

/// <summary>
/// US-035 (Bloquear fechamento com item pendente) — lê <c>pendingItemsOnClose</c> de
/// <c>TenantConfig.Operation</c> (JSONB livre, ADR-032) e resolve os itens que ainda impedem o
/// fechamento da comanda: <c>status</c> diferente de <see cref="OrderItemStatus.Served"/> e
/// <see cref="OrderItemStatus.Cancelled"/> (US-035 §8 — inclui <see cref="OrderItemStatus.Ready"/>:
/// item pronto mas ainda não entregue à mesa também bloqueia, cenário Gherkin "Fechamento
/// bloqueado"). Mesma convenção de <c>Nexora.Application.Orders.Support.ServiceFeePolicy</c>/
/// <c>Nexora.Application.Catalog.Availability.BusinessDayPolicy</c>: chave de configuração de
/// PRODUTO (nunca condicional por tenant, ADR-013), com um default seguro quando ausente/malformada.
/// </summary>
/// <remarks>
/// [DECISÃO/DESVIO DOCUMENTADO] Dois nomes concorrentes existem na documentação do produto para
/// esta mesma configuração: os documentos de domínio genéricos (03-Modelo-de-Dados.md §3.3,
/// Domain/01-Plataforma-e-Identidade.md, Domain/12-Seeds-e-Dados-Iniciais.md) mostram
/// <c>"blockCloseWithPendingItems": true</c> — um booleano simples, de uma passada anterior da
/// documentação. A US-035 (mais específica e mais recente, §3.1/§4/§8) já desenhou o comportamento
/// como três modos configuráveis — <c>operation.pendingItemsOnClose = BLOCK | WARN | IGNORE</c> —
/// exatamente o que RN-017 (hipótese, US-035 §5) pede: "bloqueio contornável com registro, não
/// absoluto". Esta implementação segue a spec da US (autoridade sobre o comportamento desta
/// história específica) — <see cref="Key"/> é <c>pendingItemsOnClose</c>, não o booleano legado.
/// Se os documentos de domínio genéricos forem atualizados depois, ajustar aqui.
/// </remarks>
/// <remarks>
/// Público (diferente de <see cref="BillQueryCoordinator"/>/<c>BillRequestCoordinator</c>, internos
/// à Application) — <see cref="MetaErrorsKey"/> precisa ser lido de
/// <c>Nexora.Api.Edge.Infrastructure.ResultExtensions</c>/<c>Nexora.Api.Cloud.Infrastructure.ResultExtensions</c>
/// (outro assembly) para montar <c>meta.pendingItems</c> na resposta 422, mesmo motivo de
/// <c>ServiceFeePolicy</c> ser público.
/// </remarks>
public static class PendingItemsClosePolicy
{
    public const string Block = "BLOCK";
    public const string Warn = "WARN";
    public const string Ignore = "IGNORE";

    /// <summary>US-035 §15: "Iniciar no modo WARN durante o piloto e endurecer só se o dado justificar" — RN-017 é hipótese.</summary>
    public const string DefaultMode = Warn;

    private const string Key = "pendingItemsOnClose";

    private static readonly string[] ValidModes = { Block, Warn, Ignore };

    /// <summary>Chave reservada em <c>Result.Errors</c> (convenção de <c>ResultExtensions.ExtractMeta</c>) que carrega o JSON dos itens pendentes até virar <c>meta.pendingItems</c> na resposta 422.</summary>
    public const string MetaErrorsKey = "pendingItemsJson";

    /// <summary>Lê <c>pendingItemsOnClose</c> (BLOCK/WARN/IGNORE) de <c>TenantConfig.Operation</c>; cai no default seguro se ausente/inválido/malformado.</summary>
    public static string ResolveMode(string? operationJson)
    {
        if (string.IsNullOrWhiteSpace(operationJson))
        {
            return DefaultMode;
        }

        try
        {
            using var document = JsonDocument.Parse(operationJson);
            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty(Key, out var value) &&
                value.ValueKind == JsonValueKind.String)
            {
                var candidate = value.GetString()?.Trim().ToUpperInvariant();
                if (candidate is not null && ValidModes.Contains(candidate, StringComparer.Ordinal))
                {
                    return candidate;
                }
            }
        }
        catch (JsonException)
        {
            // Operation malformado — cai no default seguro, mesmo espírito de BusinessDayPolicy/ServiceFeePolicy.
        }

        return DefaultMode;
    }

    /// <summary>Conveniência DB-aware — carrega o <c>TenantConfig</c> do tenant e resolve o modo, mesmo padrão de <see cref="BillQueryCoordinator.ResolveFeePercentAsync"/>.</summary>
    public static async Task<string> ResolveModeAsync(IApplicationDbContext db, Guid tenantId, CancellationToken cancellationToken)
    {
        var tenantConfig = await db.TenantConfigs
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.TenantId == tenantId, cancellationToken);
        return ResolveMode(tenantConfig?.Operation);
    }

    /// <summary>
    /// Itens que ainda impedem o fechamento (US-035 §8: status diferente de SERVED e CANCELLED) —
    /// reaproveita <see cref="BillQueryCoordinator.ItemName"/> para não duplicar a montagem do
    /// nome "produto + variante" (mesmo texto usado na divisão da conta, US-027).
    /// </summary>
    public static List<BillPendingItemResponse> FindPendingForClose(IReadOnlyList<OrderItem> items) =>
        items
            .Where(i => i.Status != OrderItemStatus.Served && i.Status != OrderItemStatus.Cancelled)
            .Select(i => new BillPendingItemResponse(i.Id, BillQueryCoordinator.ItemName(i), OrderItemStatusLabels.ToWireStatus(i.Status)))
            .ToList();

    /// <summary>
    /// Empacota a lista de pendentes na convenção "chave reservada dentro de Errors" que
    /// <c>ResultExtensions.ExtractMeta</c> (Api.Edge/Api.Cloud) já usa para <c>itemIndex</c>/
    /// <c>groupId</c>/<c>variantId</c> etc. — aqui o valor é o JSON serializado da lista inteira
    /// (não um escalar simples), porque o contrato da US-035 §7 exige um array de objetos
    /// (<c>meta.pendingItems: [{ name, status }]</c>), não um valor único.
    /// </summary>
    public static IReadOnlyDictionary<string, string[]> BuildMetaErrors(IReadOnlyList<BillPendingItemResponse> pending) =>
        new Dictionary<string, string[]> { [MetaErrorsKey] = new[] { JsonSerializer.Serialize(pending) } };

    /// <summary>
    /// Ação sensível catalogada em <c>SensitiveActionCatalog</c> (ADR-023) para "prosseguir com o
    /// fechamento mesmo com item pendente" — mesmo valor usado tanto no <c>AuditLog</c> de negócio
    /// quanto na chamada a <see cref="IAuthorizationTokenValidator.ValidateAsync"/>.
    /// </summary>
    public const string CloseWithPendingAction = "CLOSE_WITH_PENDING";

    /// <summary>
    /// Núcleo único da checagem BLOCK/WARN/IGNORE (US-035 §3.1/§8), compartilhado pelos DOIS pontos
    /// de "fechamento" já existentes hoje neste código (na ausência de US-052): a transição para
    /// BILL_REQUESTED (<c>BillRequestCoordinator.RequestAsync</c>) e o registro de pagamento parcial
    /// (<c>RegisterPartialPaymentCommandHandler</c>). Resolve o modo, decide se prossegue e — no
    /// caso <c>BLOCK</c> autorizado — grava o <see cref="AuditLog"/> de negócio
    /// (<c>action=CLOSE_WITH_PENDING</c>, autor+autorizador+motivo, cenário Gherkin "Fechamento
    /// autorizado mesmo com pendência"). Devolve a lista de pendentes em <see cref="Result{T}.Value"/>
    /// em todo sucesso (vazia em <c>IGNORE</c> ou sem pendência; preenchida em <c>WARN</c> e em
    /// <c>BLOCK</c> autorizado — para o chamador decidir se expõe como aviso) — só falha
    /// (422 <see cref="ApiErrorCodes.PendingItems"/>) quando o modo é <c>BLOCK</c> e não há
    /// autorização válida.
    /// </summary>
    public static async Task<Result<List<BillPendingItemResponse>>> EnforceAsync(
        IApplicationDbContext db,
        IAuthorizationTokenValidator authorizationValidator,
        Guid tenantId,
        Guid? storeId,
        Guid sessionId,
        string? authorizationToken,
        string? reason,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var items = await BillQueryCoordinator.LoadItemsAsync(db, sessionId, cancellationToken);
        var pending = FindPendingForClose(items);
        if (pending.Count == 0)
        {
            return Result<List<BillPendingItemResponse>>.Success(pending);
        }

        var mode = await ResolveModeAsync(db, tenantId, cancellationToken);

        if (mode == Ignore)
        {
            return Result<List<BillPendingItemResponse>>.Success(new List<BillPendingItemResponse>());
        }

        if (mode == Warn)
        {
            return Result<List<BillPendingItemResponse>>.Success(pending);
        }

        // BLOCK.
        var grant = await authorizationValidator.ValidateAsync(authorizationToken, CloseWithPendingAction, cancellationToken);
        if (grant.IsFailure)
        {
            return Result<List<BillPendingItemResponse>>.Failure(
                "Há itens que ainda não foram entregues.", ApiErrorCodes.PendingItems, BuildMetaErrors(pending));
        }

        db.AuditLogs.Add(AuditLog.Create(
            tenantId,
            action: CloseWithPendingAction,
            entity: "table_session",
            occurredAt: now,
            storeId: storeId,
            actorId: grant.Value!.ActorId,
            authorizedBy: grant.Value.AuthorizedBy,
            entityId: sessionId,
            reason: reason,
            after: JsonSerializer.Serialize(new { pendingItems = pending })));

        return Result<List<BillPendingItemResponse>>.Success(pending);
    }
}
