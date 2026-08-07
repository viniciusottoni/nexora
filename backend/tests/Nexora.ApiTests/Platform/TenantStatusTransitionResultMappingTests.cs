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
/// US-153 "Ciclo de vida do estabelecimento" — mesmo padrão de
/// <see cref="TenantsDirectoryResultMappingTests"/>: prova a tradução
/// <c>Result&lt;TenantStatusTransitionResponse&gt;</c> → <see cref="IActionResult"/> (incluindo os
/// três códigos de erro específicos do contrato desta história) sem precisar de host HTTP completo.
/// </summary>
public sealed class TenantStatusTransitionResultMappingTests
{
    [Fact]
    public void Metodo_TransitionStatus_Exige_A_Policy_PlatformAdmin()
    {
        var method = typeof(TenantsController).GetMethod(nameof(TenantsController.TransitionStatus));

        method.Should().NotBeNull();
        var authorize = method!.GetCustomAttribute<AuthorizeAttribute>();

        authorize.Should().NotBeNull("o ciclo de vida do estabelecimento é exclusivo do administrador de plataforma (US-153 §1)");
        authorize!.Policy.Should().Be("PlatformAdmin");
    }

    [Fact]
    public void Sucesso_Vira_200_Com_O_Corpo_No_Formato_Do_Contrato()
    {
        var response = new TenantStatusTransitionResponse(
            Guid.NewGuid(), "ACTIVE", "SUSPENDED", 2, DateTimeOffset.UtcNow);

        var result = global::Nexora.Application.Abstractions.Messaging.Result<TenantStatusTransitionResponse>.Success(response);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    [Fact]
    public void Transicao_Invalida_Vira_409_Recuperavel()
    {
        var result = global::Nexora.Application.Abstractions.Messaging.Result<TenantStatusTransitionResponse>.Failure(
            "Não é possível mudar de CANCELLED para ACTIVE.",
            Nexora.Shared.Errors.ApiErrorCodes.TenantStatusTransitionInvalid);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["code"].Should().Be(Nexora.Shared.Errors.ApiErrorCodes.TenantStatusTransitionInvalid);
        problem.Extensions["recoverable"].Should().Be(true);
    }

    [Fact]
    public void Conflito_De_Concorrencia_Vira_409_Recuperavel()
    {
        var result = global::Nexora.Application.Abstractions.Messaging.Result<TenantStatusTransitionResponse>.Failure(
            "Este estabelecimento foi alterado por outra sessão. Recarregue e tente novamente.",
            Nexora.Shared.Errors.ApiErrorCodes.ConcurrencyConflict);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["code"].Should().Be(Nexora.Shared.Errors.ApiErrorCodes.ConcurrencyConflict);
    }

    [Fact]
    public void Motivo_Ausente_Vira_422()
    {
        var result = global::Nexora.Application.Abstractions.Messaging.Result<TenantStatusTransitionResponse>.Failure(
            "O motivo da transição é obrigatório.",
            Nexora.Shared.Errors.ApiErrorCodes.ReasonRequired);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["code"].Should().Be(Nexora.Shared.Errors.ApiErrorCodes.ReasonRequired);
    }

    private static DefaultHttpContext CreateHttpContext() => new()
    {
        Request = { Path = "/v1/platform/tenants/00000000-0000-0000-0000-000000000000/status-transitions" },
    };
}
