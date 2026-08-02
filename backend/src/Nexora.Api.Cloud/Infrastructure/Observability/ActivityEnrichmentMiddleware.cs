using System.Diagnostics;
using Nexora.Application.Abstractions.Security;

namespace Nexora.Api.Cloud.Infrastructure.Observability;

/// <summary>
/// Adiciona <c>tenant.id</c>/<c>store.id</c>/<c>device.id</c>/<c>user.id</c> como tags no
/// <see cref="Activity.Current"/> de toda requisição autenticada (ADR-022) — sem isto, o trace
/// exportado via OTLP (<c>AddAspNetCoreInstrumentation</c>, configurado em Program.cs) não
/// carrega nenhuma dimensão de negócio, só método/rota/status HTTP. Registrado DEPOIS de
/// <c>UseAuthentication</c>/<c>UseAuthorization</c> — precisa que <c>ICurrentTenantContext</c> já
/// tenha o que ler do <c>HttpContext.User</c>. Gêmeo do middleware equivalente em
/// <c>Nexora.Api.Edge</c> — mantenha os dois consistentes.
/// </summary>
public sealed class ActivityEnrichmentMiddleware
{
    private readonly RequestDelegate _next;

    public ActivityEnrichmentMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context, ICurrentTenantContext tenantContext)
    {
        var activity = Activity.Current;
        if (activity is not null)
        {
            if (tenantContext.TenantId is { } tenantId)
            {
                activity.SetTag("tenant.id", tenantId);
            }

            if (tenantContext.StoreId is { } storeId)
            {
                activity.SetTag("store.id", storeId);
            }

            if (tenantContext.DeviceId is { } deviceId)
            {
                activity.SetTag("device.id", deviceId);
            }

            if (tenantContext.UserId is { } userId)
            {
                activity.SetTag("user.id", userId);
            }
        }

        return _next(context);
    }
}
