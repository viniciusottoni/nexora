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
/// US-155 "Proprietários, usuários iniciais e convites" — mesmo padrão de
/// <see cref="TenantPlanResultMappingTests"/>/<see cref="TenantStatusTransitionResultMappingTests"/>:
/// prova a tradução <c>Result</c>/<c>Result&lt;T&gt;</c> → <see cref="IActionResult"/> dos endpoints
/// de titularidade sem precisar de host HTTP completo.
/// </summary>
/// <remarks>
/// Os testes marcados "VERMELHO PROPOSITAL" documentam a MESMA lacuna já aceita pela US-154
/// (<c>TenantPlanResultMappingTests.PlanNotAvailable_...</c>): nenhum código novo de
/// <c>ApiErrorCodes.Ownership.cs</c> está (ainda) no <c>switch</c> de <c>ResultExtensions.MapErrorCode</c>
/// (arquivo hotspot, integração central não editada nesta tarefa — ver docstring dele e o relatório
/// final). Até essa integração, esses códigos caem no catch-all 500 em vez do status documentado.
/// <see cref="ApiErrorCodes.ReasonRequired"/> é a ÚNICA exceção: já existe e já está mapeado (US-153),
/// então o teste que o exercita passa hoje.
/// </remarks>
public sealed class TenantOwnershipResultMappingTests
{
    [Theory]
    [InlineData(nameof(TenantsController.Ownership))]
    [InlineData(nameof(TenantsController.CreateOwnerInvite))]
    [InlineData(nameof(TenantsController.RevokeOwnerInvite))]
    [InlineData(nameof(TenantsController.TransferOwnership))]
    [InlineData(nameof(TenantsController.UnlockOwnership))]
    public void Endpoints_De_Ownership_Exigem_A_Policy_PlatformAdmin(string methodName)
    {
        var method = typeof(TenantsController).GetMethod(methodName);

        method.Should().NotBeNull();
        var authorize = method!.GetCustomAttribute<AuthorizeAttribute>();

        authorize.Should().NotBeNull("acesso inicial do proprietário é decisão exclusiva da plataforma (US-155 §1), sem equivalente self-service");
        authorize!.Policy.Should().Be("PlatformAdmin");
    }

    [Fact]
    public void Ownership_Sucesso_Vira_200_Com_O_Corpo_No_Formato_Do_Contrato()
    {
        var response = new TenantOwnershipResponse(
            new TenantOwnershipOwnerResponse(Guid.NewGuid(), "Dona Betinha", "d***@example.com", "INVITED"),
            Array.Empty<TenantOwnershipInviteResponse>(),
            Array.Empty<TenantOwnershipTransferHistoryResponse>());

        var result = global::Nexora.Application.Abstractions.Messaging.Result<TenantOwnershipResponse>.Success(response);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    [Fact]
    public void CreateOwnerInvite_Sucesso_Vira_200_Com_O_Corpo_No_Formato_Do_Contrato()
    {
        var response = new CreateOwnerInviteResponse(Guid.NewGuid(), "owner@example.com", DateTimeOffset.UtcNow.AddHours(72));

        var result = global::Nexora.Application.Abstractions.Messaging.Result<CreateOwnerInviteResponse>.Success(response);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    [Fact]
    public void RevokeOwnerInvite_Sucesso_Vira_204()
    {
        var result = global::Nexora.Application.Abstractions.Messaging.Result.Success();

        var actionResult = result.ToActionResult(CreateHttpContext());

        actionResult.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public void TenantNotFound_Vira_404()
    {
        var result = global::Nexora.Application.Abstractions.Messaging.Result<TenantOwnershipResponse>.Failure(
            "Estabelecimento não encontrado.", Nexora.Shared.Errors.ApiErrorCodes.TenantNotFound);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void ReasonRequired_Ja_Mapeado_Vira_422()
    {
        var result = global::Nexora.Application.Abstractions.Messaging.Result.Failure(
            "O motivo é obrigatório.", Nexora.Shared.Errors.ApiErrorCodes.ReasonRequired);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
    }

    /// <summary>VERMELHO PROPOSITAL (ver docstring da classe) — esperado 404, hoje cai em 500.</summary>
    [Fact]
    public void OwnershipInviteNotFound_Deveria_Virar_404_Mas_Ainda_Nao_Foi_Integrado_Ao_Mapeamento_Central()
    {
        var result = global::Nexora.Application.Abstractions.Messaging.Result.Failure(
            "Convite não encontrado.", Nexora.Shared.Errors.ApiErrorCodes.OwnershipInviteNotFound);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    /// <summary>VERMELHO PROPOSITAL — esperado 422, hoje cai em 500.</summary>
    [Fact]
    public void OwnershipEmailAlreadyInUse_Deveria_Virar_422_Mas_Ainda_Nao_Foi_Integrado_Ao_Mapeamento_Central()
    {
        var result = global::Nexora.Application.Abstractions.Messaging.Result<CreateOwnerInviteResponse>.Failure(
            "Este e-mail já pertence a outro usuário.", Nexora.Shared.Errors.ApiErrorCodes.OwnershipEmailAlreadyInUse);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
    }

    /// <summary>VERMELHO PROPOSITAL — esperado 422, hoje cai em 500.</summary>
    [Fact]
    public void OwnershipSameOwner_Deveria_Virar_422_Mas_Ainda_Nao_Foi_Integrado_Ao_Mapeamento_Central()
    {
        var result = global::Nexora.Application.Abstractions.Messaging.Result<TransferTenantOwnershipResponse>.Failure(
            "O novo proprietário precisa ser diferente do atual.", Nexora.Shared.Errors.ApiErrorCodes.OwnershipSameOwner);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
    }

    /// <summary>VERMELHO PROPOSITAL — esperado 404, hoje cai em 500.</summary>
    [Fact]
    public void OwnershipTargetUserNotFound_Deveria_Virar_404_Mas_Ainda_Nao_Foi_Integrado_Ao_Mapeamento_Central()
    {
        var result = global::Nexora.Application.Abstractions.Messaging.Result<TransferTenantOwnershipResponse>.Failure(
            "Usuário não encontrado.", Nexora.Shared.Errors.ApiErrorCodes.OwnershipTargetUserNotFound);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    /// <summary>VERMELHO PROPOSITAL — esperado 422, hoje cai em 500.</summary>
    [Fact]
    public void OwnershipOwnerNotBlocked_Deveria_Virar_422_Mas_Ainda_Nao_Foi_Integrado_Ao_Mapeamento_Central()
    {
        var result = global::Nexora.Application.Abstractions.Messaging.Result<UnlockOwnerAccessResponse>.Failure(
            "O proprietário não está bloqueado.", Nexora.Shared.Errors.ApiErrorCodes.OwnershipOwnerNotBlocked);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
    }

    private static DefaultHttpContext CreateHttpContext() => new()
    {
        Request = { Path = "/v1/platform/tenants/00000000-0000-0000-0000-000000000000/ownership" },
    };
}
