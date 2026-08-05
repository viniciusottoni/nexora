using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Platform;

namespace Nexora.Application.Provisioning.Queries.ListBusinessTemplates;

/// <summary>Porta de <c>GET /v1/platform/templates</c> (US-142 §7) — lista os modelos ATIVOS, para o seletor da tela de provisionamento.</summary>
public sealed record ListBusinessTemplatesQuery : IQuery<BusinessTemplateListResponse>;
