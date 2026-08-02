using Nexora.Application.Installations.Abstractions;
using Microsoft.Extensions.Options;

namespace Nexora.Infrastructure.Installations;

/// <summary>Configuração — porta de <c>CLOUD_PUBLIC_URL</c>.</summary>
public sealed class CloudPublicUrlSettings
{
    public const string SectionName = "Installations:Cloud";

    public string PublicUrl { get; set; } = string.Empty;
}

public sealed class CloudEndpointOptions : ICloudEndpointOptions
{
    public CloudEndpointOptions(IOptions<CloudPublicUrlSettings> options)
    {
        PublicUrl = options.Value.PublicUrl;
        if (string.IsNullOrWhiteSpace(PublicUrl))
        {
            throw new InvalidOperationException(
                "Installations:Cloud:PublicUrl não configurado (equivalente a CLOUD_PUBLIC_URL no original).");
        }
    }

    public string PublicUrl { get; }
}
