using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Branding;

namespace Nexora.Application.Branding.Queries.GetPublicBranding;

/// <summary>Porta de <c>GET /public/branding?host=...</c> — pública, resolve o tenant pelo domínio customizado.</summary>
public sealed record GetPublicBrandingQuery(string Host) : IQuery<BrandingResponse>;
