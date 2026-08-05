namespace Nexora.Contracts.Platform;

/// <summary>
/// Contratos do catálogo de modelos de negócio (US-142 §7) — vivem em <c>Nexora.Contracts</c>
/// (ADR-039: só referencia <c>Nexora.Domain</c>). <c>Config</c>/<c>Seeds</c> continuam como JSON
/// bruto (a mesma forma persistida em <c>business_template.config</c>/<c>.seeds</c>): o front decide
/// como apresentar/editar, sem esta camada precisar conhecer a forma interna
/// (<c>Nexora.Application.Provisioning.BusinessTemplateConfigDto</c> etc., que Contracts nunca
/// referencia).
/// </summary>
public sealed record BusinessTemplateSummaryResponse(string Code, string Name, int Version);

public sealed record BusinessTemplateListResponse(IReadOnlyList<BusinessTemplateSummaryResponse> Data);

/// <summary>Detalhe completo de <c>GET /v1/platform/templates/{code}</c> — corpo também aceito (sem <c>IsActive</c>/<c>CreatedAt</c>/<c>UpdatedAt</c>) por <c>PUT /v1/platform/templates/{code}</c> via <see cref="UpdateBusinessTemplateRequest"/>.</summary>
public sealed record BusinessTemplateDetailResponse(
    string Code,
    string Name,
    int Version,
    bool IsActive,
    string ConfigJson,
    string SeedsJson,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>Corpo de <c>PUT /v1/platform/templates/{code}</c> — edição pela Replay (US-142 §4, cenário "Atualização de modelo"): incrementa a versão, tenants já provisionados guardam a versão antiga aplicada e não são alterados.</summary>
public sealed record UpdateBusinessTemplateRequest(string Name, string ConfigJson, string SeedsJson);
