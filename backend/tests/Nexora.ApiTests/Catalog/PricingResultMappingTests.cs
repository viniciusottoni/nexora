using Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Catalog;
using Nexora.Shared.Errors;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Nexora.ApiTests.Catalog;

/// <summary>
/// US-014 (Preço por canal de venda) — prova que os códigos de erro novos do módulo
/// (<c>Nexora.Shared.Errors.ApiErrorCodes.Pricing.cs</c>) estão mapeados em
/// <c>Nexora.Api.Cloud.Infrastructure.ResultExtensions</c> (ADR-021: "código não catalogado cai no
/// catch-all 500, nunca em 400 silencioso"). Arquivo NOVO — não edita
/// <c>CatalogResultMappingTests.cs</c>, mesmo espírito de suite.
/// </summary>
/// <remarks>
/// NOTA DE INTEGRAÇÃO — LEIA ANTES DE ASSUMIR QUE ESTA SUITE ESTÁ QUEBRADA: no momento em que este
/// arquivo foi escrito, <c>ResultExtensions.MapErrorCode</c> não tinha NENHUM caso para os códigos
/// de <c>ApiErrorCodes.Pricing.cs</c> (arquivo é exclusivo desta tarefa; <c>ResultExtensions.cs</c>
/// é proibido de editar em paralelo, ver o comentário no topo dele). Por isso os cinco testes
/// abaixo FALHAM até que alguém acrescente, no <c>switch</c> de <c>MapErrorCode</c>, os casos
/// sugeridos em cada teste (status HTTP + recoverable). Depois de plugado, esta suite deve passar
/// sem nenhuma outra mudança.
/// </remarks>
public sealed class PricingResultMappingTests
{
    /// <summary>Sugestão: 404, não recuperável (mesma família de VariantNotFound/CategoryNotFound do resto do catálogo).</summary>
    [Fact]
    public void Variante_Da_Tabela_De_Preco_Nao_Encontrada_Deve_Virar_404_Nao_Recuperavel()
    {
        var result = Result<VariantPriceTableResponse>.Failure("Variante não encontrada.", ApiErrorCodes.PriceTableVariantNotFound);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["code"].Should().Be(ApiErrorCodes.PriceTableVariantNotFound);
        problem.Extensions["recoverable"].Should().Be(false);
    }

    /// <summary>Sugestão: 404, não recuperável.</summary>
    [Fact]
    public void Categoria_Do_Reajuste_Em_Massa_Nao_Encontrada_Deve_Virar_404_Nao_Recuperavel()
    {
        var result = Result<BulkAdjustPricesResponse>.Failure("Categoria não encontrada.", ApiErrorCodes.PriceTableCategoryNotFound);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["code"].Should().Be(ApiErrorCodes.PriceTableCategoryNotFound);
        problem.Extensions["recoverable"].Should().Be(false);
    }

    /// <summary>Sugestão: 400, recuperável (cliente pode corrigir o canal e reenviar).</summary>
    [Fact]
    public void Canal_Invalido_Deve_Virar_400_Recuperavel()
    {
        var result = Result<VariantPriceTableResponse>.Failure("Canal de venda inválido.", ApiErrorCodes.PriceTableChannelInvalid);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["code"].Should().Be(ApiErrorCodes.PriceTableChannelInvalid);
        problem.Extensions["recoverable"].Should().Be(true);
    }

    /// <summary>Sugestão: 400, recuperável (mesmo canal duas vezes na mesma chamada — cliente ajusta o payload e reenvia).</summary>
    [Fact]
    public void Canal_Duplicado_Deve_Virar_400_Recuperavel()
    {
        var result = Result<VariantPriceTableResponse>.Failure("Cada canal só pode ser definido uma vez por chamada.", ApiErrorCodes.PriceTableChannelDuplicated);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["code"].Should().Be(ApiErrorCodes.PriceTableChannelDuplicated);
        problem.Extensions["recoverable"].Should().Be(true);
    }

    /// <summary>Sugestão: 422, recuperável (gestor pode escolher um percentual menor e tentar de novo).</summary>
    [Fact]
    public void Reajuste_Que_Resultaria_Em_Preco_Negativo_Deve_Virar_422_Recuperavel()
    {
        var result = Result<BulkAdjustPricesResponse>.Failure(
            "O reajuste resultaria em preço negativo para ao menos um item da categoria.", ApiErrorCodes.PriceBulkAdjustNegativeResult);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["code"].Should().Be(ApiErrorCodes.PriceBulkAdjustNegativeResult);
        problem.Extensions["recoverable"].Should().Be(true);
    }

    private static DefaultHttpContext CreateHttpContext() => new()
    {
        Request = { Path = "/v1/catalog/prices/bulk-adjust" },
    };
}
