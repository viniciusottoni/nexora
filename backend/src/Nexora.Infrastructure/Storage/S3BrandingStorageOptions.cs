namespace Nexora.Infrastructure.Storage;

/// <summary>Configuração do object storage S3-compatível usado para mídia de marca (ver <see cref="S3BrandingStorage"/>).</summary>
public sealed class S3BrandingStorageOptions
{
    public const string SectionName = "BrandingStorage";

    public string Endpoint { get; set; } = string.Empty;
    public string CdnBaseUrl { get; set; } = string.Empty;
    public string Bucket { get; set; } = string.Empty;
    public string Region { get; set; } = "us-east-1";
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
}
