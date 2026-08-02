using System.Diagnostics;
using Nexora.Application.Abstractions.Security;
using Nexora.Shared.Errors;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Cloud.Infrastructure.Auth;

/// <summary>
/// Adaptador fino de <see cref="IAuthSessionActivityGuard"/> para o pipeline HTTP (US-004, gap
/// "encerramento de sessão inativa configurável"). Roda em toda requisição autenticada que carregue
/// a claim <c>ses</c> (login por senha/refresh, cloud) — sem <c>TenantId</c>/<c>SessionId</c>
/// resolvidos (ex.: os próprios endpoints de login/refresh), não há sessão nenhuma para checar, e a
/// requisição segue normal. Registrado depois de <c>UseAuthentication()</c>/antes de
/// <c>UseAuthorization()</c>: sessão inativa nega ANTES de qualquer policy de permissão avaliar
/// (evita vazar, via a diferença entre 401 e 403, se o operador teria ou não permissão para a
/// rota). Espelha <c>Nexora.Api.Edge.Infrastructure.Auth.SessionActivityMiddleware</c> — mesma
/// duplicação deliberada de <c>ActivityEnrichmentMiddleware</c>/<c>ResultExtensions</c> (ADR-039:
/// nenhum dos dois projetos de Api pode ser referenciado pelo outro).
/// </summary>
public sealed class SessionActivityMiddleware
{
    private readonly RequestDelegate _next;

    public SessionActivityMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentTenantContext tenantContext, IAuthSessionActivityGuard guard)
    {
        if (tenantContext.TenantId is not { } tenantId || tenantContext.SessionId is not { } sessionId)
        {
            await _next(context);
            return;
        }

        var result = await guard.EnforceAsync(tenantId, sessionId, context.RequestAborted);
        if (result.IsFailure)
        {
            await WriteProblemAsync(context, result.Code ?? ApiErrorCodes.AuthSessionIdleTimeout, result.Error!);
            return;
        }

        await _next(context);
    }

    private static async Task WriteProblemAsync(HttpContext context, string code, string message)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Type = $"https://docs.nexora.app/errors/{code.ToLowerInvariant().Replace('_', '-')}",
            Title = message,
            Detail = message,
            Instance = context.Request.Path.Value is { Length: > 0 } path ? path : "/",
        };
        problem.Extensions["code"] = code;
        problem.Extensions["recoverable"] = false;
        problem.Extensions["requiresAuthorization"] = false;
        problem.Extensions["traceId"] = Activity.Current?.TraceId.ToHexString() ?? Guid.NewGuid().ToString("N");

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem, context.RequestAborted);
    }
}
