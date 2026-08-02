using System.Diagnostics;
using Nexora.Application.Abstractions.Security;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Nexora.Application.Abstractions.Behaviors;

/// <summary>
/// Log estruturado por command/query, com duração — ADR-022 (correlação e tracing). Inclui
/// <c>traceId</c>/<c>tenantId</c>/<c>storeId</c>/<c>deviceId</c> como propriedades estruturadas em
/// TODA linha (campos obrigatórios do formato de log do ADR-022 §Log estruturado) — o gap
/// original: nenhum desses campos aparecia no log de comando/query, só no que a instrumentação
/// automática do ASP.NET Core (span HTTP) já produzia por conta própria.
/// </summary>
/// <remarks>
/// Usa parâmetros nomeados do próprio <see cref="ILogger"/> (não <c>Serilog.Context.LogContext</c>)
/// deliberadamente: <c>Nexora.Application</c> só pode referenciar
/// <c>Microsoft.Extensions.Logging.Abstractions</c> (ADR-039, tabela de fronteiras do CLAUDE.md) —
/// adicionar o pacote <c>Serilog</c> aqui só para usar <c>LogContext.PushProperty</c> ampliaria a
/// superfície de dependência da Application sem necessidade: os parâmetros nomeados abaixo já
/// chegam como propriedades estruturadas em qualquer sink compatível com
/// <c>Microsoft.Extensions.Logging</c> (Serilog incluso, via <c>WriteTo.Console(new
/// CompactJsonFormatter())</c> ou similar configurado em Program.cs).
/// </remarks>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentTenantContext _tenantContext;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger, ICurrentTenantContext tenantContext)
    {
        _logger = logger;
        _tenantContext = tenantContext;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();
        var traceId = Activity.Current?.TraceId.ToHexString();
        var tenantId = _tenantContext.TenantId;
        var storeId = _tenantContext.StoreId;
        var deviceId = _tenantContext.DeviceId;

        _logger.LogInformation(
            "Iniciando {RequestName} TraceId={TraceId} TenantId={TenantId} StoreId={StoreId} DeviceId={DeviceId}",
            requestName, traceId, tenantId, storeId, deviceId);
        try
        {
            var response = await next();
            _logger.LogInformation(
                "Concluído {RequestName} em {ElapsedMs}ms TraceId={TraceId} TenantId={TenantId} StoreId={StoreId} DeviceId={DeviceId}",
                requestName, stopwatch.ElapsedMilliseconds, traceId, tenantId, storeId, deviceId);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Falha inesperada em {RequestName} após {ElapsedMs}ms TraceId={TraceId} TenantId={TenantId} StoreId={StoreId} DeviceId={DeviceId}",
                requestName, stopwatch.ElapsedMilliseconds, traceId, tenantId, storeId, deviceId);
            throw;
        }
    }
}
