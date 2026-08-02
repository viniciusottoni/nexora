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
/// US-010 (Cadastrar categorias e produtos) — prova que os códigos de erro novos do módulo
/// (<c>Nexora.Shared.Errors.ApiErrorCodes.Catalog.cs</c>) estão mapeados em
/// <c>Nexora.Api.Cloud.Infrastructure.ResultExtensions</c> (ADR-021: "código não catalogado cai no
/// catch-all 500, nunca em 400 silencioso"). Mesmo espírito de
/// <c>Nexora.ApiTests.Stations.StationsResultMappingTests</c>.
/// </summary>
public sealed class CatalogResultMappingTests
{
    [Fact]
    public void Categoria_Nao_Encontrada_Vira_404_Com_Codigo_Estavel()
    {
        var result = Result<CategoryResponse>.Failure("Categoria não encontrada.", ApiErrorCodes.CategoryNotFound);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["code"].Should().Be(ApiErrorCodes.CategoryNotFound);
        problem.Extensions["recoverable"].Should().Be(false);
    }

    [Fact]
    public void Produto_Nao_Encontrado_Vira_404_Com_Codigo_Estavel()
    {
        var result = Result<ProductResponse>.Failure("Produto não encontrado.", ApiErrorCodes.ProductNotFound);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["code"].Should().Be(ApiErrorCodes.ProductNotFound);
    }

    [Fact]
    public void Categoria_Do_Produto_Inexistente_Vira_422_Recuperavel()
    {
        var result = Result<ProductResponse>.Failure("Categoria não encontrada.", ApiErrorCodes.ProductCategoryNotFound);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["code"].Should().Be(ApiErrorCodes.ProductCategoryNotFound);
        problem.Extensions["recoverable"].Should().Be(true);
    }

    [Fact]
    public void Praca_Do_Produto_Inexistente_Vira_422_Recuperavel()
    {
        var result = Result<ProductResponse>.Failure("Praça não encontrada.", ApiErrorCodes.ProductStationNotFound);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["code"].Should().Be(ApiErrorCodes.ProductStationNotFound);
    }

    /// <summary>Cenário Gherkin "Ordenação do cardápio" (US-010 §4) — conjunto de reordenação incompleto/divergente.</summary>
    [Fact]
    public void Conjunto_De_Reordenacao_Divergente_Vira_422_Recuperavel()
    {
        var result = Result.Failure("A lista enviada não corresponde às categorias existentes.", ApiErrorCodes.CatalogReorderSetMismatch);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["code"].Should().Be(ApiErrorCodes.CatalogReorderSetMismatch);
        problem.Extensions["recoverable"].Should().Be(true);
    }

    [Fact]
    public void Armazenamento_De_Midia_Indisponivel_Vira_503_Recuperavel()
    {
        var result = Result<PrepareProductImageUploadResponse>.Failure(
            "Armazenamento de mídia de produto não configurado.", ApiErrorCodes.ProductMediaStorageUnavailable);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["code"].Should().Be(ApiErrorCodes.ProductMediaStorageUnavailable);
        problem.Extensions["recoverable"].Should().Be(true);
    }

    /// <summary>Cardápio público (<c>GET /v1/public/menu</c>) sem tenant correspondente ao host — 404, nunca 403 (ADR-021, recurso de outro tenant nunca vaza).</summary>
    [Fact]
    public void Cardapio_Publico_Sem_Tenant_Correspondente_Vira_404()
    {
        var result = Result<PublicMenuResponse>.Failure("Estabelecimento não encontrado.", ApiErrorCodes.PublicMenuTenantNotFound);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["code"].Should().Be(ApiErrorCodes.PublicMenuTenantNotFound);
    }

    /// <summary>US-011 — variante referenciada em <c>PATCH/GET .../variants/:id</c> etc. não encontrada.</summary>
    [Fact]
    public void Variante_Nao_Encontrada_Vira_404_Com_Codigo_Estavel()
    {
        var result = Result<VariantResponse>.Failure("Variante não encontrada.", ApiErrorCodes.VariantNotFound);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["code"].Should().Be(ApiErrorCodes.VariantNotFound);
        problem.Extensions["recoverable"].Should().Be(false);
    }

    /// <summary>US-011 — canal inválido em <c>SetVariantPrice</c>/<c>CreateVariant</c> vira 400 recuperável (o cliente pode corrigir e reenviar).</summary>
    [Fact]
    public void Canal_De_Preco_Invalido_Vira_400_Recuperavel()
    {
        var result = Result<PriceResponse>.Failure("Canal de venda inválido.", ApiErrorCodes.PriceChannelInvalid);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["code"].Should().Be(ApiErrorCodes.PriceChannelInvalid);
        problem.Extensions["recoverable"].Should().Be(true);
    }

    [Fact]
    public void Sucesso_De_Categoria_Vira_200_Com_O_Valor_No_Corpo()
    {
        var response = new CategoryResponse(Guid.NewGuid(), "Pizzas Salgadas", null, 1, true, ProductCount: 0);
        var result = Result<CategoryResponse>.Success(response);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    private static DefaultHttpContext CreateHttpContext() => new()
    {
        Request = { Path = "/v1/catalog/categories" },
    };
}
