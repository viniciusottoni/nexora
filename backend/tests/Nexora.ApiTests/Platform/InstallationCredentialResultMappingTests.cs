extern alias ApiCloud;
using System.Reflection;
using ApiCloud::Nexora.Api.Cloud.Controllers;
using ApiCloud::Nexora.Api.Cloud.Infrastructure;
using Nexora.Contracts.Platform;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Nexora.ApiTests.Platform;

/// <summary>
/// US-156 "Recuperação do provisionamento e token de instalação" — mesmo padrão de
/// <see cref="TenantOwnershipResultMappingTests"/>/<see cref="TenantPlanResultMappingTests"/>:
/// prova a tradução <c>Result</c>/<c>Result&lt;T&gt;</c> → <see cref="IActionResult"/> dos novos
/// endpoints sem precisar de host HTTP completo.
/// </summary>
/// <remarks>
/// Os testes marcados "VERMELHO PROPOSITAL" documentam a MESMA lacuna já aceita pelas US-154/155:
/// nenhum código novo de <c>ApiErrorCodes.InstallationCredentials.cs</c> está (ainda) no
/// <c>switch</c> de <c>ResultExtensions.MapErrorCode</c> (arquivo hotspot, integração central não
/// editada nesta tarefa — ver docstring dele e o relatório final). Até essa integração, esses
/// códigos caem no catch-all 500 em vez do status documentado no contrato da US-156.
/// <see cref="Nexora.Shared.Errors.ApiErrorCodes.InstallationNotFound"/> é a ÚNICA exceção usada
/// aqui que já existe e já está mapeada (US-002) — o teste que o exercita passa hoje.
/// </remarks>
public sealed class InstallationCredentialResultMappingTests
{
    [Theory]
    [InlineData(nameof(PlatformInstallationsController.ReissueToken))]
    [InlineData(nameof(PlatformInstallationsController.RevokeToken))]
    public void Endpoints_De_Credencial_Exigem_A_Policy_PlatformAdmin(string methodName)
    {
        // A policy está no [Authorize] de CLASSE (PlatformInstallationsController.cs) — herdada
        // por todo método do partial, inclusive os desta tarefa (mesma convenção do restante do
        // controller, ver PlatformInstallationsController.List/Diagnostics/Incidents).
        var controllerAuthorize = typeof(PlatformInstallationsController).GetCustomAttribute<AuthorizeAttribute>();
        controllerAuthorize.Should().NotBeNull();
        controllerAuthorize!.Policy.Should().Be("PlatformAdmin");

        var method = typeof(PlatformInstallationsController).GetMethod(methodName);
        method.Should().NotBeNull($"a action {methodName} deveria existir no partial Tokens.cs");
    }

    [Fact]
    public void GetTenantDeploymentStatus_Endpoint_Exige_A_Policy_PlatformAdmin()
    {
        var method = typeof(TenantsController).GetMethod(nameof(TenantsController.Deployment));

        method.Should().NotBeNull();
        var authorize = method!.GetCustomAttribute<AuthorizeAttribute>();

        authorize.Should().NotBeNull("checklist de recuperação de provisionamento é exclusivo do administrador de plataforma (US-156 §1)");
        authorize!.Policy.Should().Be("PlatformAdmin");
    }

    [Fact]
    public void ReissueToken_Sucesso_Vira_200_Com_O_Corpo_No_Formato_Do_Contrato()
    {
        var response = new ReissueInstallationTokenResponse(
            Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(24), "raw-token-mostrado-uma-vez", "./install.sh --tenant=x --token=y");

        var result = global::Nexora.Application.Abstractions.Messaging.Result<ReissueInstallationTokenResponse>.Success(response);

        var actionResult = result.ToActionResult(CreateHttpContext());

        // ToActionResult(Result<T>) sempre devolve 200 em sucesso — o controller (PlatformInstallationsController.Tokens.cs)
        // é quem sobe explicitamente para 201 quando result.IsSuccess, então este teste cobre só o
        // caminho de FALHA do Result genérico; o 201 de sucesso é melhor coberto por um teste de host
        // completo (fora do escopo desta suíte, que testa só o mapeamento de ERRO isolado).
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    [Fact]
    public void RevokeToken_Sucesso_Vira_204()
    {
        var result = global::Nexora.Application.Abstractions.Messaging.Result.Success();

        var actionResult = result.ToActionResult(CreateHttpContext());

        actionResult.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public void InstallationNotFound_Ja_Mapeado_Vira_404()
    {
        var result = global::Nexora.Application.Abstractions.Messaging.Result<ReissueInstallationTokenResponse>.Failure(
            "Instalação não encontrada.", Nexora.Shared.Errors.ApiErrorCodes.InstallationNotFound);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    /// <summary>VERMELHO PROPOSITAL (ver docstring da classe) — esperado 409 (contrato da US-156), hoje cai em 500.</summary>
    [Fact]
    public void InstallationAlreadyRegistered_Deveria_Virar_409_Mas_Ainda_Nao_Foi_Integrado_Ao_Mapeamento_Central()
    {
        var result = global::Nexora.Application.Abstractions.Messaging.Result<ReissueInstallationTokenResponse>.Failure(
            "Esta instalação já concluiu o pareamento.", Nexora.Shared.Errors.ApiErrorCodes.InstallationAlreadyRegistered);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }

    /// <summary>VERMELHO PROPOSITAL — esperado 404, hoje cai em 500.</summary>
    [Fact]
    public void InstallationCredentialNotFound_Deveria_Virar_404_Mas_Ainda_Nao_Foi_Integrado_Ao_Mapeamento_Central()
    {
        var result = global::Nexora.Application.Abstractions.Messaging.Result.Failure(
            "Credencial de instalação não encontrada.", Nexora.Shared.Errors.ApiErrorCodes.InstallationCredentialNotFound);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    private static DefaultHttpContext CreateHttpContext() => new()
    {
        Request = { Path = "/v1/platform/installations/00000000-0000-0000-0000-000000000000/tokens" },
    };
}
