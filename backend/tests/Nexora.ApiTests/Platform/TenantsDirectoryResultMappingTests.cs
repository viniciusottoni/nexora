extern alias ApiCloud;
using System.Reflection;
using ApiCloud::Nexora.Api.Cloud.Controllers;
using ApiCloud::Nexora.Api.Cloud.Infrastructure;
using Nexora.Contracts.Tenants;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Nexora.ApiTests.Platform;

/// <summary>
/// US-151 "Diretório de estabelecimentos com busca e filtros" — mesmo padrão de
/// <c>ReleasesResultMappingTests</c>/<c>SupportAccessResultMappingTests</c>: prova a tradução
/// <c>Result&lt;TenantDirectoryListResponse&gt;</c> → <see cref="IActionResult"/> sem precisar de
/// um host HTTP completo.
/// </summary>
/// <remarks>
/// O cenário Gherkin "Usuário sem autorização global → 403 sem vazar nenhum campo" (US-151 §4) NÃO
/// é coberto aqui por um teste de host HTTP completo (<c>WebApplicationFactory&lt;ApiCloud::Program&gt;</c>):
/// não existe precedente desse padrão para <c>Nexora.Api.Cloud</c> nesta suíte (as únicas duas
/// classes que sobem <c>WebApplicationFactory</c> — <c>TableMapHubTests</c>/<c>KdsHubTests</c> —
/// testam <c>Nexora.Api.Edge</c>, que não carrega as dependências extras de plataforma do Cloud:
/// e-mail/SMTP, armazenamento S3 de branding, VAPID/web push). Construir esse host do zero só para
/// este teste era desproporcional ao valor marginal: o 403 em si é produzido pelo middleware de
/// autorização do PRÓPRIO ASP.NET Core (policy <c>PlatformAdmin</c>, <c>RequireClaim("plt","admin")</c>,
/// registrada em <c>Program.cs</c>) ANTES da action rodar — por construção, nenhum campo de
/// <see cref="TenantDirectoryListResponse"/> chega a existir nesse caminho, então "não vaza campo"
/// é garantido pela ausência de execução da action, não por uma checagem de payload. O risco real de
/// regressão é o atributo <c>[Authorize(Policy = "PlatformAdmin")]</c> ser removido do método
/// <c>List</c> por engano — é exatamente esse risco que
/// <see cref="Metodo_List_Exige_A_Policy_PlatformAdmin"/> cobre, por reflexão.
/// </remarks>
public sealed class TenantsDirectoryResultMappingTests
{
    [Fact]
    public void Metodo_List_Exige_A_Policy_PlatformAdmin()
    {
        var method = typeof(TenantsController).GetMethod(nameof(TenantsController.List));

        method.Should().NotBeNull();
        var authorize = method!.GetCustomAttribute<AuthorizeAttribute>();

        authorize.Should().NotBeNull("o diretório de estabelecimentos é exclusivo do administrador de plataforma (US-151 §4)");
        authorize!.Policy.Should().Be("PlatformAdmin");
    }

    [Fact]
    public void Sucesso_Vira_200_Com_O_Corpo_No_Formato_Do_Contrato()
    {
        var entry = new TenantDirectoryEntryResponse(
            Guid.NewGuid(), "Dona Betinha", "dona-betinha", "ACTIVE", "COMPLETO", "dono@example.com",
            1, 1, "OK", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var appliedFilters = new TenantDirectoryAppliedFiltersResponse(
            "betinha", new[] { "ACTIVE" }, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(),
            null, null, "attention", 25);
        var response = new TenantDirectoryListResponse(new[] { entry }, null, appliedFilters);

        var result = global::Nexora.Application.Abstractions.Messaging.Result<TenantDirectoryListResponse>.Success(response);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    [Fact]
    public void Falha_De_Validacao_Vira_400_Recuperavel()
    {
        var result = global::Nexora.Application.Abstractions.Messaging.Result<TenantDirectoryListResponse>.Failure(
            "Revise os campos indicados e tente novamente.",
            Nexora.Shared.Errors.ApiErrorCodes.ValidationError,
            new Dictionary<string, string[]> { ["limit"] = new[] { "O limite deve estar entre 1 e 100." } });

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var problem = objectResult.Value.Should().BeOfType<ValidationProblemDetails>().Subject;
        problem.Extensions["code"].Should().Be(Nexora.Shared.Errors.ApiErrorCodes.ValidationError);
        problem.Extensions["recoverable"].Should().Be(true);
    }

    private static DefaultHttpContext CreateHttpContext() => new()
    {
        Request = { Path = "/v1/platform/tenants" },
    };
}
