namespace Nexora.Contracts.Tenants;

/// <summary>
/// US-157 · Central operacional, auditoria e atalhos de suporte —
/// <c>GET /v1/platform/tenants/{id}/administrative-timeline</c>. Agrega, em ordem cronológica,
/// fatos já persistidos por outras histórias (criação, status — US-153, plano — US-154, proprietário
/// — US-155, credenciais de instalação — US-156, domínio — US-143, suporte — US-145, incidente —
/// US-140) SEM alterar nenhuma fonte (RN-004: só leitura projetada). Diferente do exemplo ilustrativo
/// da especificação (que mostra <c>summary</c> como objeto <c>{ from, to }</c>), <see cref="AdministrativeTimelineEntryResponse.Summary"/>
/// é uma frase pronta em português — mesma convenção já estabelecida por <c>AuditLogEntry.summary</c>
/// (packages/contracts/src/audit.ts): "a UI NUNCA deve renderizar JSON bruto ao gestor".
/// </summary>
public sealed record AdministrativeTimelineActorResponse(Guid Id, string Name);

public sealed record AdministrativeTimelineEntryResponse(
    string Type,
    DateTimeOffset OccurredAt,
    AdministrativeTimelineActorResponse? Actor,
    string Origin,
    string Reason,
    string? CorrelationId,
    string Summary);

public sealed record AdministrativeTimelineListResponse(
    IReadOnlyList<AdministrativeTimelineEntryResponse> Data,
    string? NextCursor);
