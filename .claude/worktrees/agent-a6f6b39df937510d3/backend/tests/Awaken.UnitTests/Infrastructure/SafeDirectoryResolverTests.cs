using Awaken.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace Awaken.UnitTests.Infrastructure;

public class SafeDirectoryResolverTests
{
    private static SafeDirectoryResolver CreateResolver(string? rootDirectory) =>
        new(new ConfigurationBuilder()
            .AddInMemoryCollection(rootDirectory is not null
                ? new Dictionary<string, string?> { ["ExerciseImport:RootDirectory"] = rootDirectory }
                : new Dictionary<string, string?>())
            .Build());

    [Fact]
    public void ValidRelativeBatchKeyResolvesToRootCombinedWithKey()
    {
        var root = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);
        var resolver = CreateResolver(root);

        var resolved = resolver.Resolve("batch-2026-01");

        resolved.Should().NotBeNull();
        resolved.Should().StartWith(root + Path.DirectorySeparatorChar);
        resolved.Should().EndWith("batch-2026-01");
    }

    // Regressão: Path.GetFullPath preserva separador final quando o input já termina com um
    // (ex.: Path.GetTempPath() SEMPRE termina com '\'/'/' no valor bruto, não trimado). Sem o
    // TrimEnd em Resolve(), a comparação StartsWith(root + separador) compara contra um
    // separador duplicado e nunca casa, fazendo Resolve() retornar null sempre que
    // RootDirectory vem com separador final configurado (cenário comum e realista).
    [Fact]
    public void RootDirectoryWithTrailingSeparator_StillResolvesSuccessfully()
    {
        var root = Path.GetTempPath(); // valor bruto, com separador final - não trimado.

        var resolver = CreateResolver(root);
        var resolved = resolver.Resolve("batch-2026-01");

        resolved.Should().NotBeNull();
        resolved.Should().EndWith("batch-2026-01");
    }

    [Fact]
    public void ValidNestedBatchKeyResolvesCorrectly()
    {
        var root = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);
        var resolver = CreateResolver(root);

        var resolved = resolver.Resolve("provider/batch-2026-01");

        resolved.Should().NotBeNull();
        resolved.Should().Contain(Path.Combine("provider", "batch-2026-01"));
    }

    [Fact]
    public void AbsolutePathBatchKeyIsRejected()
    {
        var root = Path.GetTempPath();
        var resolver = CreateResolver(root);

        var resolved = resolver.Resolve(@"C:\Windows\System32");

        resolved.Should().BeNull();
    }

    [Fact]
    public void DoubleDotTraversalIsRejected()
    {
        var root = Path.GetTempPath();
        var resolver = CreateResolver(root);

        var resolved = resolver.Resolve("../secret");

        resolved.Should().BeNull();
    }

    [Fact]
    public void EtcPasswdTraversalIsRejected()
    {
        var root = Path.GetTempPath();
        var resolver = CreateResolver(root);

        var resolved = resolver.Resolve("../../etc/passwd");

        resolved.Should().BeNull();
    }

    [Fact]
    public void EmptyBatchKeyIsRejected()
    {
        var root = Path.GetTempPath();
        var resolver = CreateResolver(root);

        var resolved = resolver.Resolve(string.Empty);

        resolved.Should().BeNull();
    }

    [Fact]
    public void WhitespaceBatchKeyIsRejected()
    {
        var root = Path.GetTempPath();
        var resolver = CreateResolver(root);

        var resolved = resolver.Resolve("   ");

        resolved.Should().BeNull();
    }

    [Fact]
    public void NullRootDirectoryReturnsNull()
    {
        var resolver = CreateResolver(null);

        var resolved = resolver.Resolve("batch-2026-01");

        resolved.Should().BeNull();
    }

    [Fact]
    public void EmptyRootDirectoryReturnsNull()
    {
        var resolver = CreateResolver(string.Empty);

        var resolved = resolver.Resolve("batch-2026-01");

        resolved.Should().BeNull();
    }

    [Fact]
    public void BatchKeyWithNullByteIsRejected()
    {
        var root = Path.GetTempPath();
        var resolver = CreateResolver(root);

        var resolved = resolver.Resolve("batch\0malicious");

        resolved.Should().BeNull();
    }

    [Fact]
    public void RootDirectoryPropertyMatchesConfiguredValue()
    {
        var root = Path.GetTempPath();
        var resolver = CreateResolver(root);

        resolver.RootDirectory.Should().Be(root);
    }

}
