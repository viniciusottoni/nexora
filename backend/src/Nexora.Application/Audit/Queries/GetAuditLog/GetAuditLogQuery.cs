using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Audit;

namespace Nexora.Application.Audit.Queries.GetAuditLog;

/// <summary>
/// US-091 — consulta filtrável da trilha de auditoria. Porta de <c>GET /v1/audit</c>. Todos os
/// filtros são combináveis (US-091 §12, "todos os filtros isolados e combinados").
/// </summary>
/// <param name="From">Início do período (inclusive), comparado a <c>occurred_at</c>.</param>
/// <param name="To">Fim do período (inclusive), comparado a <c>occurred_at</c>.</param>
/// <param name="ActorId">Quem executou a ação.</param>
/// <param name="AuthorizedBy">Quem autorizou a ação, quando houve elevação.</param>
/// <param name="Action">Código exato da ação (ex.: <c>DISCOUNT_APPLIED</c>).</param>
/// <param name="Entity">Tipo da entidade afetada (ex.: <c>order</c>).</param>
/// <param name="EntityId">Id da entidade afetada — liga a consulta a um caso específico.</param>
/// <param name="MinAmount">
/// Valor mínimo (heurística: primeiro campo numérico encontrado entre <c>discount</c>/<c>amount</c>/
/// <c>newAmount</c>/<c>total</c> no JSONB <c>after</c> — não há campo tipado único de "valor" na
/// trilha hoje, mesma simplificação já documentada em <c>AuditLog.Before</c>/<c>After</c>).
/// </param>
/// <param name="Limit">Tamanho da página (máximo 200).</param>
/// <param name="Cursor">Cursor opaco da página anterior — nulo/vazio pede a primeira página.</param>
public sealed record GetAuditLogQuery(
    DateTimeOffset? From,
    DateTimeOffset? To,
    Guid? ActorId,
    Guid? AuthorizedBy,
    string? Action,
    string? Entity,
    Guid? EntityId,
    decimal? MinAmount,
    int Limit,
    string? Cursor) : IQuery<AuditLogListResponse>;
