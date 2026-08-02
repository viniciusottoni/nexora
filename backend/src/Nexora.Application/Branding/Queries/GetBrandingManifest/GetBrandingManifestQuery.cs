using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Branding;

namespace Nexora.Application.Branding.Queries.GetBrandingManifest;

/// <summary>Porta de <c>GET /tenant/branding.webmanifest?host=...</c> — pública.</summary>
public sealed record GetBrandingManifestQuery(string Host) : IQuery<BrandingManifestResponse>;
