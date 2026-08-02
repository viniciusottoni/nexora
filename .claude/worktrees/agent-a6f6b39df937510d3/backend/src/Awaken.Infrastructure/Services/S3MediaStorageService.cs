using Amazon.S3;
using Amazon.S3.Model;
using Awaken.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Awaken.Infrastructure.Services;

/// <summary>
/// US-236 — implementação real de <see cref="IMediaStorageService"/> sobre um bucket compatível com S3
/// (ADR-024). O <see cref="IAmazonS3"/> é injetado (configurado com o endpoint do bucket em
/// <see cref="DependencyInjection"/>) para permitir testes com mock, sem depender de credenciais reais.
/// </summary>
public class S3MediaStorageService(IAmazonS3 s3Client, IConfiguration configuration) : IMediaStorageService
{
    public async Task<string> UploadAsync(
        string key,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var bucketName = configuration["Storage:Bucket"];
        var publicBaseUrl = configuration["Storage:PublicBaseUrl"];

        if (string.IsNullOrWhiteSpace(bucketName))
            throw new InvalidOperationException("Storage:Bucket is not configured.");
        if (string.IsNullOrWhiteSpace(publicBaseUrl))
            throw new InvalidOperationException("Storage:PublicBaseUrl is not configured.");

        var request = new PutObjectRequest
        {
            BucketName = bucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false,
        };

        await s3Client.PutObjectAsync(request, cancellationToken);

        return $"{publicBaseUrl.TrimEnd('/')}/{key}";
    }
}
