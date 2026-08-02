using System.Security.Cryptography;
using System.Text;
using Nexora.Application.Abstractions.Storage;
using Microsoft.Extensions.Options;

namespace Nexora.Infrastructure.Storage;

/// <summary>
/// Gera URL de upload PUT pré-assinada (SigV4) para um bucket S3-compatível — nenhum byte de
/// mídia passa pela API, o cliente sobe direto para o object storage. Porta exata de
/// <c>s3-branding-storage.ts</c> do NestJS original.
/// </summary>
public sealed class S3BrandingStorage : IBrandingStorage
{
    private const int ExpiresSeconds = 600;

    private static readonly IReadOnlyDictionary<string, string> BaseNameByKind = new Dictionary<string, string>
    {
        ["LOGO_LIGHT"] = "logo-light",
        ["LOGO_DARK"] = "logo-dark",
        ["FAVICON"] = "favicon",
        ["PWA_ICON"] = "icon"
    };

    private static readonly IReadOnlyDictionary<string, string> ExtensionByContentType = new Dictionary<string, string>
    {
        ["image/svg+xml"] = "svg",
        ["image/png"] = "png",
        ["image/jpeg"] = "jpg",
        ["image/webp"] = "webp"
    };

    private readonly S3BrandingStorageOptions _options;

    public S3BrandingStorage(IOptions<S3BrandingStorageOptions> options)
    {
        _options = options.Value;
    }

    public Task<BrandingUpload> CreateUploadAsync(BrandingUploadRequest request, CancellationToken cancellationToken = default)
    {
        AssertConfigured();

        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddSeconds(ExpiresSeconds);
        var key = ObjectKey(request);

        var uploadUrl = PresignedPutUrl(key, now);
        var publicUrl = $"{StripTrailingSlash(_options.CdnBaseUrl)}/{key}";

        return Task.FromResult(new BrandingUpload(uploadUrl, publicUrl, expiresAt));
    }

    private string PresignedPutUrl(string key, DateTimeOffset now)
    {
        var dateTime = now.UtcDateTime.ToString("yyyyMMddTHHmmssZ");
        var date = dateTime[..8];
        var scope = $"{date}/{_options.Region}/s3/aws4_request";
        var path = $"/{EncodePath(_options.Bucket)}/{EncodePath(key)}";
        var endpointUri = new Uri(_options.Endpoint);

        var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["X-Amz-Algorithm"] = "AWS4-HMAC-SHA256",
            ["X-Amz-Credential"] = $"{_options.AccessKeyId}/{scope}",
            ["X-Amz-Date"] = dateTime,
            ["X-Amz-Expires"] = ExpiresSeconds.ToString(),
            ["X-Amz-SignedHeaders"] = "host"
        };
        var query = string.Join('&', parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));

        var canonicalRequest = string.Join(
            '\n',
            "PUT",
            path,
            query,
            $"host:{endpointUri.Host}\n",
            "host",
            "UNSIGNED-PAYLOAD");

        var stringToSign = string.Join('\n', "AWS4-HMAC-SHA256", dateTime, scope, Sha256Hex(canonicalRequest));
        var signature = Convert.ToHexString(SigningKey(_options.SecretAccessKey, date, _options.Region, stringToSign)).ToLowerInvariant();

        return $"{StripTrailingSlash(_options.Endpoint)}{path}?{query}&X-Amz-Signature={signature}";
    }

    private void AssertConfigured()
    {
        var required = new[] { _options.Endpoint, _options.CdnBaseUrl, _options.Bucket, _options.Region, _options.AccessKeyId, _options.SecretAccessKey };
        if (required.Any(string.IsNullOrEmpty))
            throw new InvalidOperationException("Armazenamento de mídia não configurado.");
    }

    private static string ObjectKey(BrandingUploadRequest request)
    {
        var baseName = BaseNameByKind.TryGetValue(request.Kind, out var name) ? name : "asset";
        var extension = ExtensionByContentType.TryGetValue(request.ContentType, out var ext) ? ext : "bin";
        return $"tenants/{request.TenantId}/branding/{baseName}.{request.Sha256.ToLowerInvariant()}.{extension}";
    }

    private static byte[] SigningKey(string secret, string date, string region, string stringToSign)
    {
        var dateKey = Hmac(Encoding.UTF8.GetBytes($"AWS4{secret}"), date);
        var regionKey = Hmac(dateKey, region);
        var serviceKey = Hmac(regionKey, "s3");
        var requestKey = Hmac(serviceKey, "aws4_request");
        return Hmac(requestKey, stringToSign);
    }

    private static byte[] Hmac(byte[] key, string content) =>
        new HMACSHA256(key).ComputeHash(Encoding.UTF8.GetBytes(content));

    private static string Sha256Hex(string content) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();

    private static string StripTrailingSlash(string value) => value.TrimEnd('/');

    private static string EncodePath(string value) =>
        string.Join('/', value.Split('/').Select(Uri.EscapeDataString));
}
