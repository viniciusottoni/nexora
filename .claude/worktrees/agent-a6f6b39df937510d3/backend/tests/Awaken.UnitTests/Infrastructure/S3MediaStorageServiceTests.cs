using Amazon.S3;
using Amazon.S3.Model;
using Awaken.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Awaken.UnitTests.Infrastructure;

/// <summary>
/// US-236 — upload de mídia (GIF 360) para o bucket S3-compatível (ADR-024). O cliente S3 é mockado via
/// <see cref="IAmazonS3"/> para não depender de credenciais reais em teste (RN-007 é responsabilidade de
/// quem chama o upload).
/// </summary>
public class S3MediaStorageServiceTests
{
    private readonly Mock<IAmazonS3> _s3Client = new();

    private static IConfiguration BuildConfiguration(
        string bucketName = "awaken-assets",
        string? publicBaseUrl = "https://media.awaken.app/api/media") =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:Bucket"] = bucketName,
                ["Storage:PublicBaseUrl"] = publicBaseUrl,
            })
            .Build();

    [Fact]
    public async Task UploadAsyncCallsPutObjectWithExpectedBucketAndKeyAndReturnsPublicUrl()
    {
        _s3Client
            .Setup(c => c.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutObjectResponse());

        var service = new S3MediaStorageService(_s3Client.Object, BuildConfiguration());
        using var content = new MemoryStream([71, 73, 70, 56]);

        var url = await service.UploadAsync("exercises/0025/360.gif", content, "image/gif", CancellationToken.None);

        url.Should().Be("https://media.awaken.app/api/media/exercises/0025/360.gif");
        _s3Client.Verify(c => c.PutObjectAsync(
            It.Is<PutObjectRequest>(r =>
                r.BucketName == "awaken-assets" &&
                r.Key == "exercises/0025/360.gif" &&
                r.ContentType == "image/gif" &&
                r.InputStream == content),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadAsyncThrowsWhenNoPublicBaseUrlIsConfigured()
    {
        var service = new S3MediaStorageService(_s3Client.Object, BuildConfiguration(publicBaseUrl: null));
        using var content = new MemoryStream([71, 73, 70, 56]);

        var act = async () => await service.UploadAsync("exercises/0025/360.gif", content, "image/gif", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Storage:PublicBaseUrl*");
        _s3Client.Verify(c => c.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UploadAsyncPropagatesExceptionWhenPutObjectFails()
    {
        _s3Client
            .Setup(c => c.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("network error"));

        var service = new S3MediaStorageService(_s3Client.Object, BuildConfiguration());
        using var content = new MemoryStream([71, 73, 70, 56]);

        var act = async () => await service.UploadAsync("exercises/0025/360.gif", content, "image/gif", CancellationToken.None);

        await act.Should().ThrowAsync<AmazonS3Exception>();
    }
}
