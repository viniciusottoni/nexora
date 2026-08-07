namespace Nexora.Contracts.Platform;

/// <summary>
/// US-157 · Central operacional, auditoria e atalhos de suporte — <c>GET /v1/platform/attention</c>
/// (fila priorizada), <c>POST /v1/platform/attention/{itemId}/acknowledgements</c> (reconhecimento
/// sem apagar o fato original, RN-004) e <c>GET /v1/platform/attention/export</c> (exportação
/// auditável de metadados administrativos). RN-015: só metadado técnico/administrativo — nenhum
/// campo aqui carrega pedido/caixa/estoque/financeiro do cliente.
/// </summary>
public sealed record AttentionActionResponse(string Kind, string Href);

public sealed record AttentionQueueItemResponse(
    string Id,
    Guid TenantId,
    string TenantName,
    string Type,
    string Severity,
    DateTimeOffset Since,
    string Reason,
    AttentionActionResponse Action);

/// <summary>
/// Metadados da coleta (Gherkin "Falha parcial": "a seção de saúde deve indicar falha e horário da
/// última coleta") — <see cref="UnavailableSources"/> vazio significa que todas as fontes
/// responderam; não vazio lista, por nome estável, qual fonte falhou (ver
/// <c>Nexora.Application.Platform.Support.PartialFailureAggregator</c>) sem derrubar o restante da
/// resposta.
/// </summary>
public sealed record AttentionQueueMetaResponse(DateTimeOffset CollectedAt, IReadOnlyList<string> UnavailableSources);

public sealed record AttentionQueueListResponse(
    IReadOnlyList<AttentionQueueItemResponse> Data,
    string? NextCursor,
    AttentionQueueMetaResponse Meta);

public sealed record AcknowledgeAttentionItemRequest(string Reason);

public sealed record AttentionAcknowledgementResponse(
    Guid Id,
    string ItemId,
    string Reason,
    DateTimeOffset AcknowledgedAt);
