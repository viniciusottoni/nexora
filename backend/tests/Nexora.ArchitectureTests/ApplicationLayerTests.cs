using Nexora.ArchitectureTests.Support;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace Nexora.ArchitectureTests;

/// <summary>
/// CLAUDE.md, tabela de fronteiras: <c>Nexora.Application</c> "pode referenciar" <c>Domain</c>,
/// MediatR, FluentValidation e <c>Microsoft.EntityFrameworkCore.Abstractions</c>¹ — "nunca
/// referencia" Npgsql/Design/Relational, ASP.NET Core, <c>Infrastructure</c>. ¹Na prática o
/// projeto referencia o pacote completo <c>Microsoft.EntityFrameworkCore</c> (não
/// <c>.Abstractions</c>, que não contém <c>DbSet&lt;T&gt;</c>) — ver nota no
/// <c>Nexora.Application.csproj</c>; o que fica proibido são os pacotes de PROVIDER
/// (<c>.Relational</c>, <c>.Design</c>, <c>Npgsql.*</c>), verificados abaixo.
/// </summary>
public sealed class ApplicationLayerTests
{
    private static readonly System.Reflection.Assembly ApplicationAssembly =
        typeof(Nexora.Application.Abstractions.Messaging.Result).Assembly;

    [Fact]
    public void Application_Nao_Referencia_Npgsql_Nem_Provider_Relacional()
    {
        var offenders = AssemblyReferenceAssertions.ReferencesMatching(
            ApplicationAssembly,
            "Npgsql",
            "Microsoft.EntityFrameworkCore.Relational",
            "Microsoft.EntityFrameworkCore.Design");

        offenders.Should().BeEmpty(
            "Application só pode referenciar Microsoft.EntityFrameworkCore.Abstractions (DbSet<T>/IQueryable), " +
            $"nunca o provider Npgsql/.Relational/.Design (ADR-039, nota¹ do CLAUDE.md) — encontrado: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void Application_Nao_Depende_De_Npgsql_Ou_Provider_Relacional_Em_Uso_De_Tipo()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                "Npgsql",
                "Microsoft.EntityFrameworkCore.Relational",
                "Microsoft.EntityFrameworkCore.Design",
                "Microsoft.EntityFrameworkCore.Migrations")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FormatFailures(result));
    }

    [Fact]
    public void Application_Nao_Referencia_AspNetCore()
    {
        var offenders = AssemblyReferenceAssertions.ReferencesMatching(ApplicationAssembly, "Microsoft.AspNetCore");

        offenders.Should().BeEmpty(
            $"Application não pode referenciar ASP.NET Core (ADR-039) — encontrado: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void Application_Nao_Depende_De_Infrastructure_Ou_Apis()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOnAny("Nexora.Infrastructure", "Nexora.Api.Edge", "Nexora.Api.Cloud")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(FormatFailures(result));
    }

    private static string FormatFailures(TestResult result) =>
        result.FailingTypes is null
            ? "falha sem detalhe de tipos"
            : string.Join(", ", result.FailingTypes.Select(t => t.FullName));
}
