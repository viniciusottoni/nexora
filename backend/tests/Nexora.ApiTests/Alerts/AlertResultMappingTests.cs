extern alias ApiCloud;
using ApiCloud::Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Alerts;
using Nexora.Shared.Errors;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Nexora.ApiTests.Alerts;

/// <summary>
/// E-08 — prova que <c>Nexora.Api.Cloud.Infrastructure.ResultExtensions</c> traduz os códigos de
/// erro do motor de alertas em RFC 7807 (ADR-021). Mesmo padrão de <c>AvailabilityResultMappingTests</c>
/// — só o catálogo do Cloud é exercitado aqui (o do Edge é idêntico por convenção do próprio arquivo,
/// ver docstring de <c>ResultExtensions</c>).
/// </summary>
public sealed class AlertResultMappingTests
{
    [Fact]
    public void Alerta_Nao_Encontrado_Vira_404_Nao_Recuperavel()
    {
        var result = Result<AlertResponse>.Failure("Alerta não encontrado.", ApiErrorCodes.AlertNotFound);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["code"].Should().Be(ApiErrorCodes.AlertNotFound);
        problem.Extensions["recoverable"].Should().Be(false);
    }

    [Fact]
    public void Alerta_Ja_Resolvido_Vira_409_Recuperavel()
    {
        var result = Result<AlertResponse>.Failure("Este alerta já foi resolvido.", ApiErrorCodes.AlertAlreadyResolved);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["recoverable"].Should().Be(true);
    }

    [Fact]
    public void Assinatura_De_Push_Invalida_Vira_400_Recuperavel()
    {
        var result = Result<SubscribePushResponse>.Failure("Endpoint inválido.", ApiErrorCodes.PushSubscriptionInvalid);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public void Sucesso_De_Alerta_Vira_200_Com_O_Valor_No_Corpo()
    {
        var response = new AlertResponse(
            Guid.NewGuid(), "ORDER_LATE", "HIGH", "order", Guid.NewGuid(), "Pedido A47 está atrasado.",
            DateTimeOffset.UtcNow, null, null, null, new[] { "WAITER" }, null, null);
        var result = Result<AlertResponse>.Success(response);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    private static DefaultHttpContext CreateHttpContext() => new()
    {
        Request = { Path = "/v1/alerts/00000000-0000-0000-0000-000000000000/acknowledge" },
    };
}
