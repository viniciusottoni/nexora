using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Platform;

namespace Nexora.Application.Provisioning.Queries.GetBusinessTemplate;

/// <summary>Porta de <c>GET /v1/platform/templates/{code}</c> (US-142 §7) — detalhe completo (config+seeds), inclusive modelos inativos (tela de manutenção da Replay).</summary>
public sealed record GetBusinessTemplateQuery(string Code) : IQuery<BusinessTemplateDetailResponse>;
