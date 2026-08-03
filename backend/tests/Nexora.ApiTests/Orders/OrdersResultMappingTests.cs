using Nexora.Api.Edge.Infrastructure;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Operation;
using Nexora.Shared.Errors;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Nexora.ApiTests.Orders;

/// <summary>
/// US-030 (Criar pedido com itens, modificadores e frações) — prova que os códigos de erro novos
/// (<c>Nexora.Shared.Errors.ApiErrorCodes.Operation.cs</c>) estão mapeados em
/// <c>Nexora.Api.Edge.Infrastructure.ResultExtensions</c> (ADR-021) e que a extração de <c>meta</c>
/// devolve exatamente o contrato da US-030 §7 (<c>{ itemIndex, groupId, groupName }</c> e
/// <c>{ variantId }</c>). Mesmo espírito de <c>Nexora.ApiTests.Catalog.CatalogResultMappingTests</c>.
/// </summary>
public sealed class OrdersResultMappingTests
{
    /// <summary>Cenário Gherkin "Grupo de modificadores obrigatório pendente" (US-030 §4/§7).</summary>
    [Fact]
    public void Modifier_Group_Required_Vira_422_Com_Meta_Exata_Do_Contrato()
    {
        var groupId = Guid.NewGuid();
        var result = Result<CreateOrderResponse>.Failure(
            "Escolha pendente em um grupo de modificadores.",
            ApiErrorCodes.ModifierGroupRequired,
            new Dictionary<string, string[]>
            {
                ["itemIndex"] = new[] { "0" },
                ["groupId"] = new[] { groupId.ToString() },
                ["groupName"] = new[] { "Tamanho" },
            });

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["code"].Should().Be(ApiErrorCodes.ModifierGroupRequired);
        problem.Extensions["recoverable"].Should().Be(true);

        var meta = problem.Extensions["meta"].Should().BeOfType<Dictionary<string, object>>().Subject;
        meta["itemIndex"].Should().Be(0);
        meta["groupId"].Should().Be(groupId.ToString());
        meta["groupName"].Should().Be("Tamanho");
    }

    [Fact]
    public void Modifier_Group_Selection_Invalid_Vira_422_Recuperavel()
    {
        var result = Result<CreateOrderResponse>.Failure(
            "Escolha pendente em um grupo de modificadores.", ApiErrorCodes.ModifierGroupSelectionInvalid);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["code"].Should().Be(ApiErrorCodes.ModifierGroupSelectionInvalid);
    }

    /// <summary>Cenário Gherkin "Produto indisponível no momento do envio" (US-030 §4/§7): <c>{ "code": "PRODUCT_UNAVAILABLE", "meta": { "variantId" } }</c>.</summary>
    [Fact]
    public void Product_Unavailable_Vira_422_Com_VariantId_No_Meta()
    {
        var variantId = Guid.NewGuid();
        var result = Result<CreateOrderResponse>.Failure(
            "Este produto está indisponível no momento.",
            ApiErrorCodes.ProductUnavailable,
            new Dictionary<string, string[]> { ["variantId"] = new[] { variantId.ToString() } });

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["code"].Should().Be(ApiErrorCodes.ProductUnavailable);

        var meta = problem.Extensions["meta"].Should().BeOfType<Dictionary<string, object>>().Subject;
        meta["variantId"].Should().Be(variantId.ToString());
    }

    [Fact]
    public void Order_Not_Accepting_Items_Vira_422_Recuperavel()
    {
        var result = Result<OrderItemResponse>.Failure(
            "Só é possível acrescentar item a um pedido confirmado, ainda em produção.", ApiErrorCodes.OrderNotAcceptingItems);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["code"].Should().Be(ApiErrorCodes.OrderNotAcceptingItems);
        problem.Extensions["recoverable"].Should().Be(true);
    }

    [Fact]
    public void Order_Not_Found_Vira_404_Nao_Recuperavel()
    {
        var result = Result<OrderResponse>.Failure("Pedido não encontrado.", ApiErrorCodes.OrderNotFound);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions["code"].Should().Be(ApiErrorCodes.OrderNotFound);
        problem.Extensions["recoverable"].Should().Be(false);
    }

    [Fact]
    public void Sucesso_Vira_200_Com_O_Valor_No_Corpo()
    {
        var order = new OrderResponse(Guid.NewGuid(), "A47", "PLACED", null, "DineIn", 60.00m, DateTimeOffset.UtcNow, Array.Empty<OrderItemResponse>());
        var result = Result<OrderResponse>.Success(order);

        var actionResult = result.ToActionResult(CreateHttpContext());

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(order);
    }

    private static DefaultHttpContext CreateHttpContext() => new()
    {
        Request = { Path = "/v1/orders" },
    };
}
