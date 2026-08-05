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
/// US-145 "Acesso de suporte auditado" — prova que <c>Nexora.Api.Cloud.Infrastructure.ResultExtensions</c>
/// traduz os quatro códigos novos de <c>Shared/Errors/ApiErrorCodes.SupportAccess.cs</c> em RFC 7807
/// (ADR-021). Mesmo padrão de <c>AlertResultMappingTests</c>.
/// </summary>
/// <remarks>
/// GAP CONHECIDO (documentado no relatório da tarefa, não corrigido aqui de propósito):
/// <c>ResultExtensions.MapErrorCode</c> ainda não lista nenhum dos quatro códigos abaixo — a
/// docstring da própria classe pede para não editá-la em paralelo com outro agente. Até essa
/// atualização central acontecer, os quatro testes abaixo falham (o código cai no catch-all 500)
/// — mesma situação já documentada no comentário "Catálogo — categorias/produtos/..." daquele
/// arquivo para outros módulos antes de serem mapeados.
/// </remarks>
public sealed class SupportAccessResultMappingTests
{
    [Fact]
    public void Acesso_De_Suporte_Nao_Encontrado_Vira_404_Nao_Recuperavel()
    {
        var result = Result<SupportAccessListResponse>.Failure(
            "Acesso de suporte não encontrado.", ApiErrorCodes.SupportAccessNotFound);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["code"].Should().Be(ApiErrorCodes.SupportAccessNotFound);
        problem.Extensions["recoverable"].Should().Be(false);
    }

    [Fact]
    public void Token_De_Suporte_Desconhecido_Vira_401_Nao_Recuperavel()
    {
        var result = Result<object>.Failure("Token de suporte inválido.", ApiErrorCodes.SupportAccessTokenNotFound);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public void Token_De_Suporte_Expirado_Vira_401_Nao_Recuperavel()
    {
        var result = Result<object>.Failure("Token de suporte expirado.", ApiErrorCodes.SupportAccessTokenExpired);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public void Token_De_Suporte_Revogado_Vira_401_Nao_Recuperavel()
    {
        var result = Result<object>.Failure("Token de suporte revogado.", ApiErrorCodes.SupportAccessTokenRevoked);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public void Sucesso_De_Concessao_Vira_200_Com_O_Valor_No_Corpo()
    {
        var response = new GrantSupportAccessResponse("raw-token", DateTimeOffset.UtcNow.AddMinutes(60), true);
        var result = Result<GrantSupportAccessResponse>.Success(response);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    private static DefaultHttpContext CreateHttpContext() => new()
    {
        Request = { Path = "/v1/platform/tenants/00000000-0000-0000-0000-000000000000/support-access" },
    };
}
