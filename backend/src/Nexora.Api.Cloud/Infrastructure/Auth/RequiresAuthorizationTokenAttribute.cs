using Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Abstractions.Security;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Nexora.Api.Cloud.Infrastructure.Auth;

/// <summary>
/// Mecanismo reutilizável de elevação pontual (ADR-023) para endpoints de negócio ainda não
/// escritos — cancelamento de item iniciado, desconto acima do limite etc. são de USes futuras do
/// E-03/E-05, fora do escopo desta correção (US-004, gap "autorização pontual é só emitida, nunca
/// validada"). Quando esse endpoint existir, decorar a action com
/// <c>[RequiresAuthorizationToken("CANCEL_STARTED_ITEM")]</c> lê o header
/// <c>X-Authorization-Token</c>, valida contra <see cref="IAuthorizationTokenValidator"/> e, em
/// sucesso, publica o <see cref="AuthorizationGrant"/> em
/// <see cref="ActionExecutingContext.HttpContext"/>.Items["AuthorizationGrant"] para o handler ler
/// quem autorizou — sem repetir a checagem de assinatura/expiração/ação em cada módulo novo.
/// Espelha <c>Nexora.Api.Edge.Infrastructure.Auth.RequiresAuthorizationTokenAttribute</c> (ADR-039:
/// nenhum dos dois projetos de Api pode ser referenciado pelo outro).
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class RequiresAuthorizationTokenAttribute : Attribute, IAsyncActionFilter
{
    public const string HeaderName = "X-Authorization-Token";
    public const string HttpContextItemKey = "AuthorizationGrant";

    public string Action { get; }

    public RequiresAuthorizationTokenAttribute(string action)
    {
        Action = action;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var validator = context.HttpContext.RequestServices.GetRequiredService<IAuthorizationTokenValidator>();
        var token = context.HttpContext.Request.Headers[HeaderName].ToString();

        var result = await validator.ValidateAsync(
            string.IsNullOrWhiteSpace(token) ? null : token, Action, context.HttpContext.RequestAborted);

        if (result.IsFailure)
        {
            context.Result = result.ToActionResult(context.HttpContext);
            return;
        }

        context.HttpContext.Items[HttpContextItemKey] = result.Value;
        await next();
    }
}
