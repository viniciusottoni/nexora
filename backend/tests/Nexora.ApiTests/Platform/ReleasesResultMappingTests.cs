extern alias ApiCloud;
using ApiCloud::Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Platform;
using Nexora.Shared.Errors;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Nexora.ApiTests.Platform;

/// <summary>
/// US-146 "Atualização controlada do parque" — prova que
/// <c>Nexora.Api.Cloud.Infrastructure.ResultExtensions</c> traduz os dois códigos novos de
/// <c>Shared/Errors/ApiErrorCodes.Releases.cs</c> em RFC 7807 (ADR-021). Mesmo padrão de
/// <c>SupportAccessResultMappingTests</c>/<c>TenantDomainResultMappingTests</c>.
/// </summary>
/// <remarks>
/// GAP CONHECIDO (documentado no relatório da tarefa, não corrigido aqui de propósito):
/// <c>ResultExtensions.MapErrorCode</c> ainda não lista nenhum dos dois códigos abaixo — a
/// docstring da própria classe pede para não editá-la em paralelo com outro agente. Até essa
/// atualização central acontecer, os dois testes abaixo falham (o código cai no catch-all 500) —
/// mesma situação já documentada em <c>SupportAccessResultMappingTests</c> para outro módulo antes
/// de ser mapeado.
/// </remarks>
public sealed class ReleasesResultMappingTests
{
    [Fact]
    public void Release_Nao_Encontrada_Vira_404_Nao_Recuperavel()
    {
        var result = Result<ReleaseRolloutResponse>.Failure("Release não encontrada.", ApiErrorCodes.ReleaseNotFound);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["code"].Should().Be(ApiErrorCodes.ReleaseNotFound);
        problem.Extensions["recoverable"].Should().Be(false);
    }

    [Fact]
    public void Rollout_Nao_Pode_Reduzir_Vira_422_Recuperavel()
    {
        var result = Result<PublishReleaseResponse>.Failure(
            "A versão já está liberada para um percentual maior.", ApiErrorCodes.ReleaseRolloutCannotDecrease);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["code"].Should().Be(ApiErrorCodes.ReleaseRolloutCannotDecrease);
        problem.Extensions["recoverable"].Should().Be(true);
    }

    [Fact]
    public void Sucesso_De_Publicacao_Vira_200_Com_O_Valor_No_Corpo()
    {
        var response = new PublishReleaseResponse(new ReleaseResponse(
            Guid.NewGuid(), "1.5.0", 10, "Notas", DateTimeOffset.UtcNow, null));
        var result = Result<PublishReleaseResponse>.Success(response);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    private static DefaultHttpContext CreateHttpContext() => new()
    {
        Request = { Path = "/v1/platform/releases" },
    };
}
