extern alias ApiCloud;
using ApiCloud::Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Stations;
using Nexora.Shared.Errors;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Nexora.ApiTests.Stations;

/// <summary>
/// US-017 (Cadastro de praças de produção) — prova que os códigos de erro novos do módulo
/// (<c>Nexora.Shared.Errors.ApiErrorCodes.Stations.cs</c>) estão mapeados em
/// <c>Nexora.Api.Cloud.Infrastructure.ResultExtensions</c> (ADR-021: "código não catalogado cai no
/// catch-all 500, nunca em 400 silencioso"). Mesmo espírito de
/// <c>Nexora.ApiTests.Idempotency.IdempotencyMiddlewareTests</c> — testa a peça de infraestrutura
/// HTTP isolada, sem precisar de um host ASP.NET Core completo nem de banco.
/// </summary>
public sealed class StationsResultMappingTests
{
    [Fact]
    public void Praca_Nao_Encontrada_Vira_404_Com_Codigo_Estavel()
    {
        var result = Result<StationResponse>.Failure("Praça não encontrada.", ApiErrorCodes.StationNotFound);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["code"].Should().Be(ApiErrorCodes.StationNotFound);
        problem.Extensions["recoverable"].Should().Be(false);
    }

    [Fact]
    public void Codigo_Ja_Existente_Vira_409_Recuperavel()
    {
        var result = Result<StationResponse>.Failure("Já existe uma praça com este código.", ApiErrorCodes.StationCodeAlreadyExists);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["code"].Should().Be(ApiErrorCodes.StationCodeAlreadyExists);
        problem.Extensions["recoverable"].Should().Be(true);
    }

    /// <summary>Cenário Gherkin "Exclusão de praça com produtos vinculados" (US-017 §4) — 422, recuperável (reatribuir e tentar de novo).</summary>
    [Fact]
    public void Praca_Com_Produtos_Vinculados_Vira_422_Recuperavel()
    {
        var result = Result.Failure("Esta praça tem 30 produto(s) vinculado(s). Reatribua os produtos antes de excluí-la.", ApiErrorCodes.StationHasLinkedProducts);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["code"].Should().Be(ApiErrorCodes.StationHasLinkedProducts);
        problem.Extensions["recoverable"].Should().Be(true);
        problem.Detail.Should().Contain("30 produto");
    }

    [Fact]
    public void Contexto_De_Loja_Ausente_Vira_403_Nao_Recuperavel()
    {
        var result = Result<StationResponse>.Failure("Loja não definida para esta requisição.", ApiErrorCodes.StationStoreContextMissing);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["code"].Should().Be(ApiErrorCodes.StationStoreContextMissing);
    }

    [Fact]
    public void Sucesso_Vira_200_Com_O_Valor_No_Corpo()
    {
        var response = new StationResponse(Guid.NewGuid(), "OVEN", "Forno", "#C1121F", 5, true, 1, true, LinkedProductCount: 0);
        var result = Result<StationResponse>.Success(response);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    private static DefaultHttpContext CreateHttpContext() => new()
    {
        Request = { Path = "/v1/catalog/stations" },
    };
}
