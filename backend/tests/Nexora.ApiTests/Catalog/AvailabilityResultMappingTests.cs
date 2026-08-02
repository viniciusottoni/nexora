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
/// US-015 (Marcar produto indisponível com propagação imediata) — prova que
/// <c>Nexora.Api.Cloud.Infrastructure.ResultExtensions</c> traduz <see cref="Result{T}"/> de
/// disponibilidade em RFC 7807 (ADR-021) corretamente. Este módulo não introduz nenhum código de
/// erro novo (ver <c>ApiErrorCodes.Availability.cs</c>) — reaproveita <c>"PRODUCT_NOT_FOUND"</c>
/// (US-010, já mapeado para 404 em <c>ResultExtensions.MapErrorCode</c>),
/// <see cref="ApiErrorCodes.ValidationError"/> e <see cref="ApiErrorCodes.TenantContextMissing"/>.
/// </summary>
public sealed class AvailabilityResultMappingTests
{
    [Fact]
    public void Sucesso_De_Disponibilidade_Vira_200_Com_O_Valor_No_Corpo()
    {
        var response = new ProductAvailabilityResponse(Guid.NewGuid(), "Pizza Calabresa", false, "Acabou o insumo", DateTimeOffset.UtcNow);
        var result = Result<ProductAvailabilityResponse>.Success(response);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    [Fact]
    public void Sucesso_De_Lista_De_Indisponiveis_Vira_200_Com_O_Valor_No_Corpo()
    {
        var response = new UnavailableProductsResponse(Array.Empty<ProductAvailabilityResponse>());
        var result = Result<UnavailableProductsResponse>.Success(response);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(response);
    }

    [Fact]
    public void Tenant_Nao_Identificado_Vira_403_Nao_Recuperavel()
    {
        var result = Result<ProductAvailabilityResponse>.Failure(
            "Não foi possível identificar o estabelecimento vinculado ao seu usuário.", ApiErrorCodes.TenantContextMissing);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["code"].Should().Be(ApiErrorCodes.TenantContextMissing);
        problem.Extensions["recoverable"].Should().Be(false);
    }

    [Fact]
    public void Motivo_De_Indisponibilidade_Vazio_Vira_400_Recuperavel_Via_Codigo_Generico_De_Validacao()
    {
        var errors = new Dictionary<string, string[]> { ["Reason"] = new[] { "Informe o motivo da indisponibilidade." } };
        var result = Result<ProductAvailabilityResponse>.Failure(
            "Revise os campos indicados e tente novamente.", ApiErrorCodes.ValidationError, errors);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var problem = objectResult.Value.Should().BeOfType<ValidationProblemDetails>().Subject;
        problem.Extensions["code"].Should().Be(ApiErrorCodes.ValidationError);
        problem.Extensions["recoverable"].Should().Be(true);
    }

    /// <summary>
    /// "PRODUCT_NOT_FOUND" (mesmo código estável usado por US-010) mapeia para 404, não
    /// recuperável — nunca 403, mesmo padrão de <c>ApiErrorCodes.CategoryNotFound</c>/<c>VariantNotFound</c>.
    /// Produzido por <c>MarkProductUnavailableCommandHandler</c>/<c>MarkProductAvailableCommandHandler</c>.
    /// </summary>
    [Fact]
    public void Produto_Nao_Encontrado_Vira_404_Nao_Recuperavel()
    {
        var result = Result<ProductAvailabilityResponse>.Failure("Produto não encontrado.", ApiErrorCodes.ProductNotFound);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["recoverable"].Should().Be(false);
    }

    private static DefaultHttpContext CreateHttpContext() => new()
    {
        Request = { Path = "/v1/catalog/products/00000000-0000-0000-0000-000000000000/availability" },
    };
}
