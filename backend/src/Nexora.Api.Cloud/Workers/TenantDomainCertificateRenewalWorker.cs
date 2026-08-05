using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.TenantDomains.Commands.RenewTenantDomainCertificate;
using Nexora.Domain.Platform;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Nexora.Api.Cloud.Workers;

/// <summary>
/// US-143 §3.1 "Emissão e renovação automática de certificado TLS" + §4 cenário "Falha de
/// renovação" — varre <c>tenant_domain</c> ativo em busca de certificados a 15 dias (ou menos) do
/// vencimento (<see cref="TenantDomain.IsCertificateExpiringSoon"/>) e despacha um comando por
/// domínio via <see cref="ISender"/>. Mesmo esqueleto de
/// <c>InstallationHealthEvaluationWorker</c>/<c>AlertEvaluationWorker</c>: varredura tenant-por-tenant
/// (<c>tenant_domain</c> tem RLS, ao contrário de <c>tenant</c>) mantendo a lógica de negócio
/// testável fora do <see cref="BackgroundService"/>.
/// </summary>
public sealed partial class TenantDomainCertificateRenewalWorker : BackgroundService
{
    /// <summary>6 horas — certificados são avaliados por dia de folga (limiar de 15 dias), não precisa de granularidade fina.</summary>
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TenantDomainCertificateRenewalWorker> _logger;

    public TenantDomainCertificateRenewalWorker(IServiceScopeFactory scopeFactory, ILogger<TenantDomainCertificateRenewalWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogVarreduraFalhou(ex);
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    /// <summary>Público — usado pelos testes para exercitar uma iteração sem esperar o intervalo.</summary>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var now = DateTimeOffset.UtcNow;
        var tenantIds = await db.Tenants.AsNoTracking().Where(t => t.DeletedAt == null).Select(t => t.Id).ToListAsync(cancellationToken);

        var renewedTotal = 0;
        foreach (var tenantId in tenantIds)
        {
            // RLS (ADR-004): tenant_domain tem tenant_isolation — sem este SET explícito (worker
            // não tem ICurrentTenantContext derivado de requisição HTTP), a leitura abaixo não
            // retornaria nenhuma linha.
            await db.SetTenantContextAsync(tenantId, cancellationToken);

            var expiringDomainIds = await db.TenantDomains.AsNoTracking()
                .Where(d => d.TenantId == tenantId && d.DeletedAt == null && d.Status == TenantDomainStatus.Active)
                .ToListAsync(cancellationToken);

            foreach (var domainId in expiringDomainIds.Where(d => d.IsCertificateExpiringSoon(now)).Select(d => d.Id))
            {
                var renewed = await sender.Send(new RenewTenantDomainCertificateCommand(tenantId, domainId), cancellationToken);
                if (renewed.IsSuccess && renewed.Value)
                {
                    renewedTotal++;
                }
                else if (renewed.IsFailure)
                {
                    LogRenovacaoFalhou(tenantId, domainId, renewed.Error);
                }
            }
        }

        return renewedTotal;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Falha ao varrer certificados de domínio próprio da plataforma.")]
    private partial void LogVarreduraFalhou(Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Falha ao renovar certificado do domínio {DomainId} (tenant {TenantId}): {Erro}")]
    private partial void LogRenovacaoFalhou(Guid tenantId, Guid domainId, string? erro);
}
