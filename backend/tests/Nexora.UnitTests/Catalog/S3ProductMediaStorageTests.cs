using FluentAssertions;
using Microsoft.Extensions.Options;
using Nexora.Application.Abstractions.Storage;
using Nexora.Infrastructure.Storage;
using Xunit;

namespace Nexora.UnitTests.Catalog;

public sealed class S3ProductMediaStorageTests
{
    [Fact]
    public async Task Deve_Gerar_Chave_Original_Deterministica_Para_Heic()
    {
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        const string hash = "ABCDEFABCDEFABCDEFABCDEFABCDEFABCDEFABCDEFABCDEFABCDEFABCDEFABCD";
        var storage = new S3ProductMediaStorage(Options.Create(new S3ProductMediaStorageOptions
        {
            Endpoint = "https://s3.example.com",
            CdnBaseUrl = "https://cdn.example.com",
            Bucket = "catalog",
            Region = "us-east-1",
            AccessKeyId = "access",
            SecretAccessKey = "secret"
        }));

        var upload = await storage.CreateUploadAsync(
            new ProductMediaUploadRequest(tenantId, productId, "image/heic", 1_000, hash));

        upload.PublicUrl.Should().Be(
            $"https://cdn.example.com/tenants/{tenantId}/products/{productId}/original.{hash.ToLowerInvariant()}.heic");
        storage.IsExpectedPublicUrl(
            new ProductMediaUploadRequest(tenantId, productId, "image/heic", 1_000, hash),
            upload.PublicUrl).Should().BeTrue();
        storage.IsExpectedPublicUrl(
            new ProductMediaUploadRequest(tenantId, productId, "image/heic", 1_000, hash),
            "https://attacker.example.com/forged.heic").Should().BeFalse();
    }
}
