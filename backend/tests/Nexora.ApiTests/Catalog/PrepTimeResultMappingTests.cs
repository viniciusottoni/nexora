extern alias ApiCloud;
using ApiCloud::Nexora.Api.Cloud.Controllers;
using ApiCloud::Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Catalog.PrepTime.Commands.ReassignProductStation;
using Nexora.Application.Catalog.PrepTime.Commands.UpdateVariantPrepTimeThresholds;
using Nexora.Application.Catalog.PrepTime.Queries.GetVariantPrepTimeAnalysis;
using Nexora.Contracts.Catalog;
using Nexora.Shared.Errors;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace Nexora.ApiTests.Catalog;

/// <summary>
/// US-016 — cobre <see cref="ProductPrepTimeController"/> mapeando <c>Result</c>/<c>Result&lt;T&gt;</c>
/// para <c>IActionResult</c> via <see cref="ResultExtensions"/> real (sem precisar de banco nem
/// de host HTTP completo, mesmo padrão de
/// <c>Nexora.ApiTests.Auth.InstallationAuthenticationHandlerTests</c>) — o <c>ISender</c> é
/// substituído (NSubstitute), então o alvo aqui é só a tradução controller -&gt; HTTP, não a
/// regra de negócio dos handlers (isso é <c>Nexora.UnitTests</c>/<c>Nexora.IntegrationTests</c>).
/// </summary>
public sealed class PrepTimeResultMappingTests
{
    private static ProductPrepTimeController CreateController(ISender sender) =>
        new(sender) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };

    [Fact]
    public async Task UpdatePrepTime_Com_Sucesso_Retorna_200_Com_O_Corpo_Do_Result()
    {
        var variantId = Guid.NewGuid();
        var response = new VariantPrepTimeResponse(variantId, 12, 15, 20);
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<UpdateVariantPrepTimeThresholdsCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<VariantPrepTimeResponse>.Success(response)));

        var result = await CreateController(sender).UpdatePrepTime(
            variantId, new UpdatePrepTimeThresholdsRequest(12, 15, 20), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(response);
        ok.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task ReassignStation_Com_Sucesso_Retorna_200_Com_O_Corpo_Do_Result()
    {
        var productId = Guid.NewGuid();
        var stationId = Guid.NewGuid();
        var response = new ProductStationResponse(productId, stationId, "FORNO", "Forno");
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<ReassignProductStationCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<ProductStationResponse>.Success(response)));

        var result = await CreateController(sender).ReassignStation(
            productId, new ReassignStationRequest(stationId), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(response);
    }

    [Fact]
    public async Task GetPrepTimeAnalysis_Com_Sucesso_Retorna_200_Com_O_Corpo_Do_Result()
    {
        var variantId = Guid.NewGuid();
        var response = new PrepTimeAnalysisResponse(variantId, 12, 15, false, 25, true, 16.4m, null, 340, 16, null);
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<GetVariantPrepTimeAnalysisQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<PrepTimeAnalysisResponse>.Success(response)));

        var result = await CreateController(sender).GetPrepTimeAnalysis(variantId, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(response);
    }

    [Fact]
    public async Task UpdatePrepTime_Com_Variante_Nao_Encontrada_Devolve_ProblemDetails_Com_O_Codigo_Certo()
    {
        var variantId = Guid.NewGuid();
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<UpdateVariantPrepTimeThresholdsCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<VariantPrepTimeResponse>.Failure(
                "Variação não encontrada.", ApiErrorCodes.PrepTimeVariantNotFound)));

        var result = await CreateController(sender).UpdatePrepTime(
            variantId, new UpdatePrepTimeThresholdsRequest(12, null, null), CancellationToken.None);

        var problem = result.Should().BeOfType<ObjectResult>().Subject;
        var body = problem.Value.Should().BeAssignableTo<ProblemDetails>().Subject;
        body.Extensions["code"].Should().Be(ApiErrorCodes.PrepTimeVariantNotFound);
    }

    /// <summary>
    /// Os três códigos de <c>ApiErrorCodes.PrepTime*</c> estão mapeados em
    /// <c>ResultExtensions.MapErrorCode</c> para 404, não recuperável — nenhum cai no catch-all 500.
    /// </summary>
    [Theory]
    [InlineData("PrepTimeVariantNotFound")]
    [InlineData("PrepTimeProductNotFound")]
    [InlineData("PrepTimeStationNotFound")]
    public async Task Falha_Com_Codigo_De_PrepTime_Mapeia_Para_404(string codeName)
    {
        var code = codeName switch
        {
            "PrepTimeVariantNotFound" => ApiErrorCodes.PrepTimeVariantNotFound,
            "PrepTimeProductNotFound" => ApiErrorCodes.PrepTimeProductNotFound,
            _ => ApiErrorCodes.PrepTimeStationNotFound,
        };

        var variantId = Guid.NewGuid();
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<UpdateVariantPrepTimeThresholdsCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result<VariantPrepTimeResponse>.Failure("Não encontrado.", code)));

        var result = await CreateController(sender).UpdatePrepTime(
            variantId, new UpdatePrepTimeThresholdsRequest(12, null, null), CancellationToken.None);

        var problem = result.Should().BeOfType<ObjectResult>().Subject;
        problem.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        var body = problem.Value.Should().BeAssignableTo<ProblemDetails>().Subject;
        body.Extensions["recoverable"].Should().Be(false);
    }
}
