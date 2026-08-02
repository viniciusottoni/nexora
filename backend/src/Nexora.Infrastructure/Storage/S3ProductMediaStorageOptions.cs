namespace Nexora.Infrastructure.Storage;

/// <summary>Configuração do object storage S3-compatível usado para foto de produto (ver <see cref="S3ProductMediaStorage"/>).</summary>
public sealed class S3ProductMediaStorageOptions
{
    public const string SectionName = "ProductMediaStorage";

    public string Endpoint { get; set; } = string.Empty;
    public string CdnBaseUrl { get; set; } = string.Empty;
    public string Bucket { get; set; } = string.Empty;
    public string Region { get; set; } = "us-east-1";
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
}
