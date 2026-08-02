using Nexora.ArchitectureTests.Support;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace Nexora.ArchitectureTests;

/// <summary>
/// CLAUDE.md, tabela de fronteiras: <c>Nexora.Domain</c> "pode referenciar" só a BCL e "nunca
/// referencia" MediatR, EF Core, ASP.NET Core, <c>Infrastructure</c>, <c>Api.*</c>. É a linha que
/// sustenta o resto do produto — garantida por ausência de referência, não por disciplina
/// (ADR-039).
/// </summary>
public sealed class DomainLayerTests
{
    private static readonly System.Reflection.Assembly DomainAssembly = typeof(Nexora.Domain.Common.DomainException).Assembly;

    [Fact]
    public void Domain_Nao_Referencia_Nenhum_Assembly_Fora_Da_BCL()
    {
        var offenders = AssemblyReferenceAssertions.NonBclReferences(DomainAssembly);

        offenders.Should().BeEmpty(
            "Nexora.Domain não pode ter ProjectReference/PackageReference além da BCL (ADR-039/ADR-036) — " +
            $"encontrado: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void Domain_Nao_Depende_De_MediatR()
    {
        var result = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOnAny("MediatR")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FormatFailures(result));
    }

    [Fact]
    public void Domain_Nao_Depende_De_EfCore_Ou_Npgsql()
    {
        var result = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Npgsql")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FormatFailures(result));
    }

    [Fact]
    public void Domain_Nao_Depende_De_AspNetCore()
    {
        var result = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOnAny("Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FormatFailures(result));
    }

    [Fact]
    public void Domain_Nao_Depende_De_Infrastructure_Ou_Apis()
    {
        var result = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOnAny("Nexora.Infrastructure", "Nexora.Api.Edge", "Nexora.Api.Cloud", "Nexora.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FormatFailures(result));
    }

    private static string FormatFailures(TestResult result) =>
        result.FailingTypes is null
            ? "falha sem detalhe de tipos"
            : string.Join(", ", result.FailingTypes.Select(t => t.FullName));
}
