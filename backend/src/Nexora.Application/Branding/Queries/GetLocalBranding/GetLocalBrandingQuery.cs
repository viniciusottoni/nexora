using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Branding;

namespace Nexora.Application.Branding.Queries.GetLocalBranding;

/// <summary>
/// Porta de <c>GET /v1/local/branding</c> (<c>Nexora.Api.Edge</c>) — US-003, gap "resolução de
/// tenant por host não funciona para web-pos/web-kds". Diferente de
/// <see cref="Nexora.Application.Branding.Queries.GetPublicBranding.GetPublicBrandingQuery"/>
/// (que resolve o tenant pelo domínio público customizado, para <c>web-menu</c>), esta consulta
/// não recebe nenhum parâmetro de host: o edge é a autoridade operacional de exatamente UM tenant
/// (ADR-004, "uma loja = um tenant"), fixado na instalação (<c>EdgeInstallationOptions</c>) e
/// exposto por <c>ICurrentTenantContext.TenantId</c> mesmo sem autenticação — POS/KDS rodam na LAN
/// da loja, onde o host HTTP nunca bate com <c>Tenant.Domain</c>.
/// </summary>
public sealed record GetLocalBrandingQuery : IQuery<BrandingResponse>;
