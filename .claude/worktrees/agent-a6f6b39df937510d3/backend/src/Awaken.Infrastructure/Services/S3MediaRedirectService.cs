using Amazon.S3;
using Amazon.S3.Model;
using Awaken.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Awaken.Infrastructure.Services;

public class S3MediaRedirectService(IAmazonS3 s3Client, IConfiguration configuration) : IMediaRedirectService
{
    private static readonly TimeSpan PresignedUrlLifetime = TimeSpan.FromHours(1);

    public string CreatePresignedReadUrl(string key)
    {
        var bucketName = configuration["Storage:Bucket"];
        if (string.IsNullOrWhiteSpace(bucketName))
            throw new InvalidOperationException("Storage:Bucket is not configured.");

        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Protocol = Protocol.HTTPS,
            Expires = DateTime.UtcNow.Add(PresignedUrlLifetime),
        };

        return s3Client.GetPreSignedURL(request);
    }
}
