using System.Text.Json;

namespace Nexora.Contracts.Audit;

/// <summary>Ator ou autorizador de uma linha da trilha — nome já resolvido, nunca só o Id (US-091 §10).</summary>
public sealed record AuditActorSummary(Guid Id, string Name);

/// <summary>Dispositivo de origem da ação, quando aplicável (edge).</summary>
public sealed record AuditDeviceSummary(Guid Id, string Label);

/// <summary>Entidade de origem (US-091 §7) — permite ao front navegar direto ao pedido/mesa/produto.</summary>
public sealed record AuditTargetSummary(string Type, Guid Id, string Label);

/// <summary>
/// Uma linha da trilha (<c>GET /v1/audit</c>). <see cref="Before"/>/<see cref="After"/> continuam
/// estruturados (não uma string JSON escapada) para o front poder detalhar sob demanda, mas
/// <see cref="Summary"/> é a frase pronta em português que a US-091 exige como exibição PADRÃO —
/// "não deve exibir JSON bruto ao gestor" (§4, cenário "Antes e depois legíveis") significa que
/// nenhuma tela usa <see cref="Before"/>/<see cref="After"/> como texto principal.
/// </summary>
public sealed record AuditLogEntryResponse(
    Guid Id,
    string Action,
    string Summary,
    AuditActorSummary? Actor,
    AuditActorSummary? AuthorizedBy,
    AuditDeviceSummary? Device,
    DateTimeOffset OccurredAt,
    AuditTargetSummary? Target,
    JsonElement? Before,
    JsonElement? After,
    string? Reason,
    string? TraceId);

public sealed record AuditLogListMeta(string? NextCursor, bool HasMore);

public sealed record AuditLogListResponse(IReadOnlyList<AuditLogEntryResponse> Data, AuditLogListMeta Meta);
