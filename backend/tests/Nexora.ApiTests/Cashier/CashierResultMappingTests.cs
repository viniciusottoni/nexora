using Nexora.Api.Edge.Infrastructure;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Cashier.Support;
using Nexora.Contracts.Cashier;
using Nexora.Shared.Errors;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Nexora.ApiTests.Cashier;

/// <summary>
/// US-055/US-056 — prova que os códigos de erro novos (<c>Nexora.Shared.Errors.ApiErrorCodes.Cashier.cs</c>)
/// estão mapeados em <c>Nexora.Api.Edge.Infrastructure.ResultExtensions</c> (ADR-021) e que a
/// extração de <c>meta</c> devolve exatamente o contrato de <c>OPEN_TABLES</c> (US-055 §7:
/// <c>meta.openSessions: [{ table, total }]</c>). Mesmo espírito de <c>OrdersResultMappingTests</c>.
/// </summary>
public sealed class CashierResultMappingTests
{
    private static readonly System.Text.Json.JsonSerializerOptions CamelCaseOptions =
        new() { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase };

    [Fact]
    public void Cash_Session_Already_Open_Vira_409_Com_SessionId_No_Meta()
    {
        var sessionId = Guid.NewGuid();
        var result = Result<CashSessionResponse>.Failure(
            "Já existe um caixa aberto para este operador neste turno.",
            ApiErrorCodes.CashSessionAlreadyOpen,
            new Dictionary<string, string[]> { ["sessionId"] = new[] { sessionId.ToString() } });

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["code"].Should().Be(ApiErrorCodes.CashSessionAlreadyOpen);
        problem.Extensions["recoverable"].Should().Be(true);

        var meta = problem.Extensions["meta"].Should().BeOfType<Dictionary<string, object>>().Subject;
        meta["sessionId"].Should().Be(sessionId.ToString());
    }

    [Fact]
    public void No_Open_Cash_Session_Vira_409_Nao_Requer_Autorizacao()
    {
        var result = Result<object>.Failure("Não há caixa aberto para este operador.", ApiErrorCodes.NoOpenCashSession);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["code"].Should().Be(ApiErrorCodes.NoOpenCashSession);
        problem.Extensions["requiresAuthorization"].Should().Be(false);
    }

    /// <summary>US-055 §7, cenário "Mesa aberta no fechamento": <c>{ "code": "OPEN_TABLES", "meta": { "openSessions": [{ "table", "total" }] } }</c>.</summary>
    [Fact]
    public void Open_Tables_Vira_422_Com_OpenSessions_No_Meta_Exatamente_Como_O_Contrato()
    {
        var openTables = new List<OpenTableSessionInfo> { new("12", 87.00m) };
        var metaJson = System.Text.Json.JsonSerializer.Serialize(openTables, CamelCaseOptions);
        var result = Result<CloseCashSessionResponse>.Failure(
            "Existem mesas ainda abertas — feche-as ou autorize o fechamento do caixa mesmo assim.",
            ApiErrorCodes.OpenTables,
            new Dictionary<string, string[]> { [CashCloseGuard.MetaErrorsKey] = new[] { metaJson } });

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["code"].Should().Be(ApiErrorCodes.OpenTables);
        problem.Extensions["recoverable"].Should().Be(true);
        problem.Extensions["requiresAuthorization"].Should().Be(true);

        var meta = problem.Extensions["meta"].Should().BeOfType<Dictionary<string, object>>().Subject;
        var openSessions = meta["openSessions"].Should().BeOfType<System.Text.Json.JsonElement>().Subject;
        openSessions.GetArrayLength().Should().Be(1);
        openSessions[0].GetProperty("table").GetString().Should().Be("12");
        // ADR-017: dinheiro trafega como STRING no JSON (MoneyJsonConverter) — nunca número puro.
        openSessions[0].GetProperty("total").GetString().Should().Be("87.00");
    }

    [Fact]
    public void Cash_Justification_Required_Vira_422_Recuperavel_Sem_Autorizacao()
    {
        var result = Result<CloseCashSessionResponse>.Failure(
            "A divergência encontrada exige uma justificativa antes de fechar o caixa.", ApiErrorCodes.CashJustificationRequired);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["code"].Should().Be(ApiErrorCodes.CashJustificationRequired);
        problem.Extensions["requiresAuthorization"].Should().Be(false);
    }

    [Fact]
    public void Cash_Session_Not_Found_Vira_404_Nao_Recuperavel()
    {
        var result = Result<CashSessionResponse>.Failure("Sessão de caixa não encontrada.", ApiErrorCodes.CashSessionNotFound);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["code"].Should().Be(ApiErrorCodes.CashSessionNotFound);
        problem.Extensions["recoverable"].Should().Be(false);
    }

    [Fact]
    public void Sucesso_Vira_200_Com_O_Valor_No_Corpo()
    {
        var session = new CashSessionResponse(Guid.NewGuid(), Guid.NewGuid(), "OPEN", 200m, DateTimeOffset.UtcNow, null, null, null, null, null);
        var result = Result<CashSessionResponse>.Success(session);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(session);
    }

    private static DefaultHttpContext CreateHttpContext() => new()
    {
        Request = { Path = "/v1/cash-sessions/current" },
    };
}
