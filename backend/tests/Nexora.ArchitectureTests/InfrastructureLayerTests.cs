using Nexora.ArchitectureTests.Support;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace Nexora.ArchitectureTests;

/// <summary>
/// CLAUDE.md, tabela de fronteiras: <c>Nexora.Infrastructure</c> "pode referenciar" <c>Domain</c>,
/// <c>Application</c>, <c>Contracts</c>, EF Core, Npgsql, StackExchange.Redis — "nunca referencia"
/// ASP.NET Core, exceto abstrações de <c>Options</c> (<c>Microsoft.Extensions.Options</c>, que não
/// é ASP.NET Core — vive em <c>Microsoft.Extensions.*</c>, então a checagem de prefixo abaixo já
/// não a pega). Esta é a regra que o gap original da auditoria de US-001 violava (o projeto tinha
/// <c>Microsoft.AspNetCore.Authentication.JwtBearer</c> não usado em <c>Nexora.Infrastructure.csproj</c>).
/// </summary>
public sealed class InfrastructureLayerTests
{
    private static readonly System.Reflection.Assembly InfrastructureAssembly =
        typeof(Nexora.Infrastructure.Persistence.AppDbContext).Assembly;

    [Fact]
    public void Infrastructure_Nao_Referencia_AspNetCore()
    {
        var offenders = AssemblyReferenceAssertions.ReferencesMatching(InfrastructureAssembly, "Microsoft.AspNetCore");

        offenders.Should().BeEmpty(
            "Infrastructure não pode referenciar ASP.NET Core, exceto abstrações de Options " +
            $"(ADR-039) — encontrado: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void Infrastructure_Nao_Depende_De_AspNetCore_Em_Uso_De_Tipo()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .Should()
            .NotHaveDependencyOnAny("Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            result.FailingTypes is null
                ? "falha sem detalhe de tipos"
                : string.Join(", ", result.FailingTypes.Select(t => t.FullName)));
    }

    [Fact]
    public void Infrastructure_Nao_Depende_De_Api_Edge_Ou_Api_Cloud()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .Should()
            .NotHaveDependencyOnAny("Nexora.Api.Edge", "Nexora.Api.Cloud")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            result.FailingTypes is null
                ? "falha sem detalhe de tipos"
                : string.Join(", ", result.FailingTypes.Select(t => t.FullName)));
    }
}
