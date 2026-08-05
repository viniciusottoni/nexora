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
/// US-154 "Gestão de planos e configuração comercial" — mesmo padrão de
/// <see cref="TenantStatusTransitionResultMappingTests"/>: prova a tradução
/// <c>Result&lt;T&gt;</c> → <see cref="IActionResult"/> dos endpoints de plano comercial sem
/// precisar de host HTTP completo.
/// </summary>
/// <remarks>
/// <see cref="PlanNotAvailable_Deveria_Virar_422_Mas_Ainda_Nao_Foi_Integrado_Ao_Mapeamento_Central"/>
/// documenta uma lacuna CONHECIDA: <c>PLAN_NOT_AVAILABLE</c> (código NOVO desta história) ainda não
/// está no <c>switch</c> de <c>ResultExtensions.MapErrorCode</c> — esse arquivo é
/// deliberadamente "não editar em paralelo por múltiplos agentes" (ver docstring da classe) e só é
/// atualizado numa integração central posterior. Até lá, o código cai no catch-all (500) em vez do
/// 422 esperado — o teste abaixo prova e documenta esse estado atual (fica VERMELHO quando o
/// código for esperado como 422; ver relatório final da tarefa).
/// </remarks>
public sealed class TenantPlanResultMappingTests
{
    [Theory]
    [InlineData(nameof(TenantsController.ListPlans))]
    [InlineData(nameof(TenantsController.GetPlan))]
    [InlineData(nameof(TenantsController.UpdatePlan))]
    [InlineData(nameof(TenantsController.ReconcilePlanConfig))]
    public void Endpoints_De_Plano_Exigem_A_Policy_PlatformAdmin(string methodName)
    {
        var method = typeof(TenantsController).GetMethod(methodName);

        method.Should().NotBeNull();
        var authorize = method!.GetCustomAttribute<AuthorizeAttribute>();

        authorize.Should().NotBeNull("plano comercial é decisão exclusiva da plataforma (US-154 §1), sem equivalente self-service");
        authorize!.Policy.Should().Be("PlatformAdmin");
    }

    [Fact]
    public void ListPlans_Sucesso_Vira_200_Com_O_Corpo_No_Formato_Do_Contrato()
    {
        var response = new PlatformPlanListResponse(new[]
        {
            new PlatformPlanSummaryResponse("COMPLETO", "Completo", true, new[] { "online_ordering" }),
        });

        var result = global::Nexora.Application.Abstractions.Messaging.Result<PlatformPlanListResponse>.Success(response);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    [Fact]
    public void GetPlan_Sucesso_Vira_200_Com_O_Corpo_No_Formato_Do_Contrato()
    {
        var response = new TenantPlanResponse(
            "GESTAO",
            new[] { "online_ordering", "kds" },
            Scheduled: null,
            Consistent: true,
            Version: 1);

        var result = global::Nexora.Application.Abstractions.Messaging.Result<TenantPlanResponse>.Success(response);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    [Fact]
    public void UpdatePlan_Com_Vigencia_Futura_Vira_200_Com_Scheduled_Preenchido()
    {
        var response = new TenantPlanUpdateResponse(
            "GESTAO", new TenantPlanScheduledResponse("COMPLETO", DateTimeOffset.UtcNow.AddDays(7)), Version: 2);

        var result = global::Nexora.Application.Abstractions.Messaging.Result<TenantPlanUpdateResponse>.Success(response);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    [Fact]
    public void TenantNotFound_Vira_404()
    {
        var result = global::Nexora.Application.Abstractions.Messaging.Result<TenantPlanResponse>.Failure(
            "Estabelecimento não encontrado.", Nexora.Shared.Errors.ApiErrorCodes.TenantNotFound);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void ConcurrencyConflict_Vira_409_Recuperavel()
    {
        var result = global::Nexora.Application.Abstractions.Messaging.Result<TenantPlanUpdateResponse>.Failure(
            "Este estabelecimento foi alterado por outra sessão. Recarregue e tente novamente.",
            Nexora.Shared.Errors.ApiErrorCodes.ConcurrencyConflict);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }

    [Fact]
    public void ReasonRequired_Vira_422()
    {
        var result = global::Nexora.Application.Abstractions.Messaging.Result<TenantPlanUpdateResponse>.Failure(
            "O motivo da mudança de plano é obrigatório.", Nexora.Shared.Errors.ApiErrorCodes.ReasonRequired);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
    }

    /// <summary>
    /// VERMELHO PROPOSITAL (ver docstring da classe) — <c>PLAN_NOT_AVAILABLE</c> ainda não foi
    /// adicionado ao <c>switch</c> de <c>ResultExtensions.MapErrorCode</c> (arquivo hotspot,
    /// integração central). Hoje cai no catch-all (500); o correto (US-154 §4, cenário "Plano
    /// desconhecido") é 422. Este teste fica falhando até essa integração acontecer — não é um bug
    /// desta tarefa, é a lacuna documentada no relatório final.
    /// </summary>
    [Fact]
    public void PlanNotAvailable_Deveria_Virar_422_Mas_Ainda_Nao_Foi_Integrado_Ao_Mapeamento_Central()
    {
        var result = global::Nexora.Application.Abstractions.Messaging.Result<TenantPlanUpdateResponse>.Failure(
            "Plano comercial não disponível.", Nexora.Shared.Errors.ApiErrorCodes.PlanNotAvailable);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
    }

    private static DefaultHttpContext CreateHttpContext() => new()
    {
        Request = { Path = "/v1/platform/tenants/00000000-0000-0000-0000-000000000000/plan" },
    };
}
