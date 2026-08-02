using Nexora.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Nexora.Infrastructure.Notifications;

/// <summary>
/// Consome <c>email_outbox</c> e entrega de fato via <see cref="IEmailDispatcher"/> — a peça que
/// faltava depois de <see cref="EmailOutboxSender"/> só enfileirar (porta do
/// <c>InvitationDeliveryWorker</c> citado na docstring de <c>IEmailSender</c>). Registrar só em
/// <c>Nexora.Api.Cloud/Program.cs</c> (<c>AddHostedService&lt;EmailOutboxDeliveryWorker&gt;()</c>) —
/// convite de dono é fluxo exclusivo de nuvem; <c>Api.Edge</c> nunca registra <see cref="EmailOutboxOptions"/>
/// nem <see cref="Application.Abstractions.Notifications.IEmailSender"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Por que iterar tenant a tenant:</b> <c>email_outbox</c> tem RLS (ADR-004, política
/// <c>tenant_isolation</c>) e este worker roda fora de qualquer requisição HTTP — não há
/// <c>ICurrentTenantContext</c> autenticado para fixar <c>app.tenant_id</c> automaticamente (o
/// <see cref="Persistence.Interceptors.TenantConnectionInterceptor"/> não teria de onde ler). A
/// única tabela sem RLS é <c>tenant</c> (raiz global), então o worker lista os tenants por ali e,
/// para cada um, fixa o contexto explicitamente com <see cref="IApplicationDbContext.SetTenantContextAsync"/>
/// (mesmo mecanismo de <c>LoginWithPasswordCommandHandler</c>/<c>ProvisionTenantCommandHandler</c>)
/// antes de consultar/atualizar as linhas daquele tenant. Custo O(tenants) por iteração — aceitável
/// na escala inicial do produto (ADR-013); se isso virar gargalo, o caminho é o papel
/// <c>platform_admin</c> (BYPASSRLS, já criado pela migration <c>EnableRowLevelSecurity</c> mas
/// ainda sem nenhuma conexão da aplicação usando-o) com uma trilha de auditoria dedicada — decisão
/// de arquitetura maior, fora do escopo desta tarefa.
/// </para>
/// <para>
/// <b>Por que não passa por MediatR/TransactionBehavior:</b> não há regra de negócio nem evento de
/// domínio aqui (ADR-006 não se aplica — entregar e-mail não é uma transição de estado do domínio,
/// é infraestrutura pura), então <see cref="IApplicationDbContext.SaveChangesAsync"/> é chamado
/// diretamente pelo worker, um lote por tenant.
/// </para>
/// </remarks>
public sealed class EmailOutboxDeliveryWorker : BackgroundService
{
    private const string PendingStatus = "PENDING";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EmailOutboxOptions _options;
    private readonly ILogger<EmailOutboxDeliveryWorker> _logger;

    public EmailOutboxDeliveryWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<EmailOutboxOptions> options,
        ILogger<EmailOutboxDeliveryWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(_options.PollingIntervalSeconds, 1));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Nunca deixa uma falha pontual (ex.: banco fora do ar) derrubar o worker — mesma
                // resiliência de SyncOutboxWorker (Api.Edge).
                _logger.LogWarning(ex, "Falha ao processar lote de e-mails pendentes de email_outbox.");
            }

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

    /// <summary>
    /// Processa um lote (todos os tenants, até <see cref="EmailOutboxOptions.BatchSize"/> por
    /// tenant) e retorna quantos registros foram efetivamente entregues. Público (não apenas
    /// <c>protected</c>/privado) de propósito — é o ponto de entrada usado pelos testes de
    /// integração para exercitar uma iteração sem esperar o intervalo de polling.
    /// </summary>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IEmailDispatcher>();

        var tenantIds = await db.Tenants
            .AsNoTracking()
            .Where(t => t.DeletedAt == null)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        var delivered = 0;

        foreach (var tenantId in tenantIds)
        {
            delivered += await DeliverPendingForTenantAsync(db, dispatcher, tenantId, cancellationToken);
        }

        return delivered;
    }

    private async Task<int> DeliverPendingForTenantAsync(
        IApplicationDbContext db, IEmailDispatcher dispatcher, Guid tenantId, CancellationToken cancellationToken)
    {
        await db.SetTenantContextAsync(tenantId, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var pending = await db.EmailOutboxes
            .Where(e => e.Status == PendingStatus && (e.NextAttemptAt == null || e.NextAttemptAt <= now))
            .OrderBy(e => e.CreatedAt)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            return 0;
        }

        var delivered = 0;
        foreach (var email in pending)
        {
            try
            {
                var variables = EmailPayloadCipher.Decrypt(email.PayloadEncrypted, _options.EncryptionKey);
                await dispatcher.SendAsync(email.Recipient, email.Template, variables, cancellationToken);
                email.MarkSent();
                delivered++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                var exhausted = email.Attempts + 1 >= _options.MaxAttempts;
                email.MarkFailed(ex.Message, exhausted ? null : now.AddSeconds(_options.RetryDelaySeconds));

                _logger.LogWarning(
                    ex,
                    "Falha ao entregar e-mail de email_outbox. EmailOutboxId={EmailOutboxId} TenantId={TenantId} Exhausted={Exhausted}",
                    email.Id, tenantId, exhausted);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return delivered;
    }
}
