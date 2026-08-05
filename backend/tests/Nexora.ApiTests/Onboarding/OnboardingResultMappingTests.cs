extern alias ApiCloud;
using ApiCloud::Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Tables.Support;
using Nexora.Shared.Errors;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Nexora.ApiTests.Onboarding;

/// <summary>
/// US-141 §7 — prova que <see cref="ApiErrorCodes.OnboardingIncomplete"/>/
/// <see cref="ApiErrorCodes.OnboardingStepNotFound"/> viram a resposta HTTP pretendida pelo
/// contrato, mesmo espírito de <c>Nexora.ApiTests.Catalog.CatalogResultMappingTests</c> e
/// <c>Nexora.ApiTests.Tables.PendingItemsResultMappingTests</c>-like (US-035).
/// </summary>
/// <remarks>
/// [GAP CONHECIDO E DOCUMENTADO] Esta tarefa foi instruída a NÃO editar
/// <c>Nexora.Api.Cloud.Infrastructure.ResultExtensions.MapErrorCode</c> (arquivo de edição
/// coordenada, ver docstring daquela classe: "não editar em paralelo por múltiplos agentes"). Sem
/// as duas entradas abaixo naquele switch, os dois testes de status ficam VERMELHOS hoje (caem no
/// catch-all 500) — deixados assim de propósito, como evidência executável do gap, em vez de
/// silenciados com <c>[Fact(Skip=...)]</c>. Adicionar, no switch de <c>MapErrorCode</c>:
/// <code>
/// ApiErrorCodes.OnboardingIncomplete =&gt; (StatusCodes.Status422UnprocessableEntity, true, false),
/// ApiErrorCodes.OnboardingStepNotFound =&gt; (StatusCodes.Status404NotFound, false, false),
/// </code>
/// Depois dessa adição os dois testes de status abaixo passam a verde sem qualquer outra mudança.
/// O teste de <see cref="Ativacao_Incompleta_Carrega_As_Chaves_Pendentes_Em_Meta"/> já passa hoje —
/// cobre só a extração de <c>meta</c> (mecanismo de <c>PendingItemsClosePolicy</c> reaproveitado,
/// independente do status HTTP).
/// </remarks>
public sealed class OnboardingResultMappingTests
{
    [Fact]
    public void Ativacao_Incompleta_Vira_422_Recuperavel()
    {
        var result = Result.Failure("Ainda há passos pendentes.", ApiErrorCodes.OnboardingIncomplete);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(
            StatusCodes.Status422UnprocessableEntity,
            "US-141 §7 — pendente de uma entrada em ResultExtensions.MapErrorCode, ver docstring desta classe");
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["code"].Should().Be(ApiErrorCodes.OnboardingIncomplete);
        problem.Extensions["recoverable"].Should().Be(true);
        problem.Extensions["requiresAuthorization"].Should().Be(false);
    }

    [Fact]
    public void Passo_Desconhecido_Vira_404_Nao_Recuperavel()
    {
        var result = Result.Failure("Passo do roteiro de implantação não encontrado.", ApiErrorCodes.OnboardingStepNotFound);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(
            StatusCodes.Status404NotFound,
            "US-141 §7 — pendente de uma entrada em ResultExtensions.MapErrorCode, ver docstring desta classe");
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["code"].Should().Be(ApiErrorCodes.OnboardingStepNotFound);
        problem.Extensions["recoverable"].Should().Be(false);
    }

    /// <summary>
    /// Cobre só a extração de <c>meta</c> — a lista de chaves pendentes (US-141 §7 <c>meta.pending</c>)
    /// chega hoje como <c>meta.pendingItems</c>, porque reaproveita o MESMO mecanismo genérico de
    /// <c>PendingItemsClosePolicy.MetaErrorsKey</c> (ver <c>ActivateTenantCommandHandler.BuildPendingMetaErrors</c>)
    /// em vez de um nome de campo dedicado — desvio documentado no mesmo lugar do gap de status acima.
    /// </summary>
    [Fact]
    public void Ativacao_Incompleta_Carrega_As_Chaves_Pendentes_Em_Meta()
    {
        var pendingStepKeys = new[] { "MENU", "TABLES" };
        var fieldErrors = new Dictionary<string, string[]>
        {
            [PendingItemsClosePolicy.MetaErrorsKey] = new[] { System.Text.Json.JsonSerializer.Serialize(pendingStepKeys) },
        };
        var result = Result.Failure("Ainda há passos pendentes.", ApiErrorCodes.OnboardingIncomplete, fieldErrors);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions.Should().ContainKey("meta");
        var meta = problem.Extensions["meta"].Should().BeAssignableTo<System.Collections.Generic.Dictionary<string, object>>().Subject;
        meta.Should().ContainKey("pendingItems");
    }

    private static DefaultHttpContext CreateHttpContext() => new()
    {
        Request = { Path = "/v1/platform/tenants/00000000-0000-0000-0000-000000000000/activate" },
    };
}
