using Nexora.Application.Installation.Commands.PollSyncHealth;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Nexora.Infrastructure.Installation;

/// <summary>
/// Porta de <c>sync-worker.ts</c> como <see cref="BackgroundService"/> (ADR-037: equivalente ao
/// <c>SyncOutboxModule</c> do Nest citado no ADR). Registrar com
/// <c>builder.Services.AddHostedService&lt;SyncOutboxWorker&gt;()</c> em
/// <c>Nexora.Api.Edge/Program.cs</c>. O worker só orquestra o intervalo — o processamento
/// de negócio (<see cref="PollSyncHealthCommand"/>) é despachado via <see cref="ISender"/>
/// resolvido por escopo a cada tick, porque <c>ISender</c>/<c>IApplicationDbContext</c> são
/// scoped e um <see cref="BackgroundService"/> é singleton (padrão oficial do ASP.NET Core para
/// serviços em segundo plano que precisam de dependências scoped).
/// </summary>
public sealed class SyncOutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EdgeInstallationIdentityOptions _options;
    private readonly ILogger<SyncOutboxWorker> _logger;

    public SyncOutboxWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<EdgeInstallationIdentityOptions> options,
        ILogger<SyncOutboxWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMilliseconds(Math.Max(_options.SyncHealthIntervalMs, 1_000));

        while (!stoppingToken.IsCancellationRequested)
        {
            await PollOnceAsync(stoppingToken);

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task PollOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        try
        {
            await sender.Send(new PollSyncHealthCommand(), cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Nunca deixa uma falha pontual (ex.: rede fora) derrubar o worker — próxima
            // iteração tenta de novo (mesma resiliência do setInterval original em TS).
            _logger.LogWarning(ex, "Falha ao consultar saúde de sincronização.");
        }
    }
}
