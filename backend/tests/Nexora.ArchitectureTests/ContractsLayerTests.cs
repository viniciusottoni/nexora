using Nexora.ArchitectureTests.Support;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace Nexora.ArchitectureTests;

/// <summary>
/// CLAUDE.md, tabela de fronteiras: <c>Nexora.Contracts</c> "pode referenciar" só <c>Domain</c> —
/// "nunca referencia" <c>Application</c>, <c>Infrastructure</c>, ASP.NET Core. Contracts é DTO
/// puro (requests/responses da API); se ele precisar de lógica de aplicação, a lógica está no
/// lugar errado.
/// </summary>
public sealed class ContractsLayerTests
{
    private static readonly System.Reflection.Assembly ContractsAssembly =
        typeof(Nexora.Contracts.Errors.ErrorResponse).Assembly;

    [Fact]
    public void Contracts_Nao_Referencia_Application_Ou_Infrastructure()
    {
        var offenders = AssemblyReferenceAssertions.ReferencesMatching(
            ContractsAssembly, "Nexora.Application", "Nexora.Infrastructure", "Nexora.Api.Edge", "Nexora.Api.Cloud");

        offenders.Should().BeEmpty(
            $"Nexora.Contracts só pode referenciar Nexora.Domain (CLAUDE.md) — encontrado: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void Contracts_Nao_Referencia_AspNetCore()
    {
        var offenders = AssemblyReferenceAssertions.ReferencesMatching(ContractsAssembly, "Microsoft.AspNetCore");

        offenders.Should().BeEmpty($"Contracts é DTO puro, sem ASP.NET Core — encontrado: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void Contracts_Nao_Depende_De_Application_Em_Uso_De_Tipo()
    {
        var result = Types.InAssembly(ContractsAssembly)
            .Should()
            .NotHaveDependencyOnAny("Nexora.Application", "Nexora.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            result.FailingTypes is null
                ? "falha sem detalhe de tipos"
                : string.Join(", ", result.FailingTypes.Select(t => t.FullName)));
    }
}
