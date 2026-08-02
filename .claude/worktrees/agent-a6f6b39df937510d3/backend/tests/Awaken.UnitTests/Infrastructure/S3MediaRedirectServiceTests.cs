using Amazon.S3;
using Amazon.S3.Model;
using Awaken.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Awaken.UnitTests.Infrastructure;

public class S3MediaRedirectServiceTests
{
    private readonly Mock<IAmazonS3> _s3Client = new();

    private static IConfiguration BuildConfiguration(string bucketName = "awaken-assets") =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:Bucket"] = bucketName,
            })
            .Build();

    [Fact]
    public void CreatePresignedReadUrlUsesConfiguredBucketAndKey()
    {
        _s3Client
            .Setup(c => c.GetPreSignedURL(It.IsAny<GetPreSignedUrlRequest>()))
            .Returns((GetPreSignedUrlRequest request) =>
                $"https://signed.example/{request.BucketName}/{request.Key}");

        var service = new S3MediaRedirectService(_s3Client.Object, BuildConfiguration());

        var url = service.CreatePresignedReadUrl("exercises/0025/360.gif");

        url.Should().Be("https://signed.example/awaken-assets/exercises/0025/360.gif");
        _s3Client.Verify(c => c.GetPreSignedURL(It.Is<GetPreSignedUrlRequest>(request =>
            request.BucketName == "awaken-assets" &&
            request.Key == "exercises/0025/360.gif")),
            Times.Once);
    }

    [Fact]
    public void CreatePresignedReadUrlThrowsWhenBucketNameIsMissing()
    {
        var service = new S3MediaRedirectService(_s3Client.Object, BuildConfiguration(bucketName: string.Empty));

        var act = () => service.CreatePresignedReadUrl("images/gold.png");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Storage:Bucket*");
        _s3Client.Verify(c => c.GetPreSignedURL(It.IsAny<GetPreSignedUrlRequest>()), Times.Never);
    }
}
