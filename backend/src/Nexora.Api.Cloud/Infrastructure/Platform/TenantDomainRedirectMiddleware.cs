using Nexora.Application.Abstractions.Platform;
using Microsoft.AspNetCore.Http;

namespace Nexora.Api.Cloud.Infrastructure.Platform;

/// <summary>
/// US-143 §3.1 "Redirecionamento do domínio padrão para o próprio" — quando o host da requisição
/// bate com o domínio padrão calculado de um tenant (<c>{slug}.{Platform:DefaultDomainSuffix}</c>,
/// ver <c>PlatformDomainOptions</c>) e esse tenant já tem um domínio próprio verificado e ativo
/// (<c>Tenant.Domain</c>, espelhado de <c>TenantDomain.IsPrimary</c> por
/// <c>VerifyTenantDomainCommandHandler</c>), redireciona (302) para o mesmo caminho no domínio
/// próprio. Só GET — redirecionar POST/PUT/PATCH/DELETE é semanticamente errado (a semântica de
/// redirecionamento HTTP para métodos com corpo é ambígua entre navegadores/clientes — 307
/// preservaria o método mas exigiria reenviar o corpo já consumido) e desnecessário (o cliente que
/// está enviando uma escrita para esse host já decidiu, deliberadamente ou não, falar com ele; só
/// a navegação de página/leitura precisa do redirecionamento transparente).
/// </summary>
public sealed class TenantDomainRedirectMiddleware
{
    private readonly RequestDelegate _next;

    public TenantDomainRedirectMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantDomainRedirectResolver resolver)
    {
        if (HttpMethods.IsGet(context.Request.Method))
        {
            var host = context.Request.Host.Host;
            if (!string.IsNullOrWhiteSpace(host))
            {
                var redirectTarget = await resolver.ResolvePrimaryDomainAsync(host.Trim().ToLowerInvariant(), context.RequestAborted);
                if (redirectTarget is not null)
                {
                    var location = $"{context.Request.Scheme}://{redirectTarget}{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}";
                    context.Response.Redirect(location, permanent: false);
                    return;
                }
            }
        }

        await _next(context);
    }
}
