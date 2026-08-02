using System.Text.Json;
using Awaken.Api.Middlewares;
using Awaken.Application.Common.Exceptions;
using Awaken.Contracts.Common;
using Awaken.Domain.Entities.Economy;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Awaken.UnitTests.Middlewares;

public class ExceptionHandlingMiddlewareTests
{
    private static async Task<(int StatusCode, ApiErrorResponse Body)> InvokeAsync(
        Exception thrown,
        string? correlationId = "corr-1",
        PathString? path = null)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = path ?? "/api/test";
        if (correlationId is not null)
        {
            context.Items["CorrelationId"] = correlationId;
        }

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw thrown,
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var json = await reader.ReadToEndAsync();
        var body = JsonSerializer.Deserialize<ApiErrorResponse>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        })!;

        return (context.Response.StatusCode, body);
    }

    [Fact]
    public async Task NotFoundException_Returns404_WithCorrelationId()
    {
        var (statusCode, body) = await InvokeAsync(new NotFoundException("User", "123"));

        statusCode.Should().Be(StatusCodes.Status404NotFound);
        body.Code.Should().Be("NOT_FOUND");
        body.CorrelationId.Should().Be("corr-1");
    }

    [Fact]
    public async Task UnauthorizedException_Returns401()
    {
        var (statusCode, body) = await InvokeAsync(new UnauthorizedException());

        statusCode.Should().Be(StatusCodes.Status401Unauthorized);
        body.Code.Should().Be("UNAUTHORIZED");
    }

    [Fact]
    public async Task UnauthorizedException_WithCustomCode_Returns401WithThatCode()
    {
        var (statusCode, body) = await InvokeAsync(
            new UnauthorizedException("INVALID_CREDENTIALS", "E-mail ou senha inválidos."));

        statusCode.Should().Be(StatusCodes.Status401Unauthorized);
        body.Code.Should().Be("INVALID_CREDENTIALS");
        body.Message.Should().Be("E-mail ou senha inválidos.");
    }

    [Fact]
    public async Task ValidationException_OnProfilePath_ReturnsInvalidProfileData()
    {
        var validationException = new ValidationException([
            new ValidationFailure("Age", "Idade deve estar entre 10 e 120 anos.")
        ]);

        var (statusCode, body) = await InvokeAsync(
            validationException,
            path: "/api/users/me/profile");

        statusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
        body.Code.Should().Be("INVALID_PROFILE_DATA");
        body.Message.Should().Be("Revise os dados informados.");
    }

    [Fact]
    public async Task ValidationException_OnOtherPath_ReturnsGenericValidationError()
    {
        var validationException = new ValidationException([
            new ValidationFailure("Email", "Email invalido.")
        ]);

        var (statusCode, body) = await InvokeAsync(validationException);

        statusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
        body.Code.Should().Be("VALIDATION_ERROR");
        body.Message.Should().Be("One or more validation errors occurred.");
    }

    [Fact]
    public async Task UnhandledException_Returns500_GenericMessage_NoStackTraceLeak()
    {
        var (statusCode, body) = await InvokeAsync(new InvalidOperationException("internal detail"));

        statusCode.Should().Be(StatusCodes.Status500InternalServerError);
        body.Code.Should().Be("INTERNAL_ERROR");
        body.Message.Should().NotContain("internal detail");
    }

    [Fact]
    public async Task MissingCorrelationId_GeneratesNewOne()
    {
        var (_, body) = await InvokeAsync(new UnauthorizedException(), correlationId: null);

        body.CorrelationId.Should().NotBeNullOrEmpty();
    }

    // US-227 / RN-005: saldo insuficiente deve retornar 422 com código dedicado,
    // não cair no 500 genérico (gap pré-existente identificado na US).
    [Fact]
    public async Task InsufficientGoldBalanceException_Returns422_WithDedicatedCode()
    {
        var (statusCode, body) = await InvokeAsync(
            new InsufficientGoldBalanceException(Guid.NewGuid(), currentBalance: 10, requestedAmount: 50));

        statusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
        body.Code.Should().Be("INSUFFICIENT_GOLD_BALANCE");
    }

    // US-227 / RN-005: conflito de concorrência esgotado deve retornar 409 controlado.
    [Fact]
    public async Task ConcurrencyConflictException_Returns409_WithDedicatedCode()
    {
        var (statusCode, body) = await InvokeAsync(
            new ConcurrencyConflictException());

        statusCode.Should().Be(StatusCodes.Status409Conflict);
        body.Code.Should().Be("CONCURRENCY_CONFLICT");
    }
}
