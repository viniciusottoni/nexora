using Nexora.Application.Abstractions.Persistence;
using Nexora.Domain.Platform;
using Nexora.IntegrationTests.Fakes;
using Nexora.IntegrationTests.Fixtures;
using Nexora.Infrastructure.Notifications;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Nexora.IntegrationTests;

/// <summary>
/// US-002, gap "convite do gestor só enfileira e-mail, nunca envia de fato" —
/// <see cref="EmailOutboxDeliveryWorker"/> é o worker que faltava. Prova, contra Postgres real com
/// RLS, que um registro pendente de <c>email_outbox</c> é decifrado, "entregue" (via um
/// <see cref="IEmailDispatcher"/> de teste, para não depender de SMTP real) e marcado como
/// <c>SENT</c>; e que uma falha de entrega grava o erro e agenda um novo <c>NextAttemptAt</c>.
/// </summary>
[Collection("Postgres")]
public sealed class EmailOutboxDeliveryWorkerIntegrationTests
{
    private const string EncryptionKey = "integration-test-email-outbox-worker-key";

    private readonly PostgresFixture _fixture;

    public EmailOutboxDeliveryWorkerIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task RunOnceAsync_Entrega_Registro_Pendente_E_Marca_Como_Sent()
    {
        var tenantId = await SeedTenantAsync();
        var recipient = $"owner-{Guid.NewGuid():N}@example.com";

        await SeedPendingEmailAsync(tenantId, recipient, "owner-invite", new Dictionary<string, string>
        {
            ["ownerName"] = "Dona Betinha",
            ["tenantName"] = "Pizzaria Dona Betinha",
            ["token"] = "abc123",
        });

        var dispatcher = new RecordingEmailDispatcher();
        var worker = BuildWorker(dispatcher);

        var delivered = await worker.RunOnceAsync(CancellationToken.None);

        delivered.Should().Be(1);
        dispatcher.Sent.Should().ContainSingle();
        dispatcher.Sent[0].Recipient.Should().Be(recipient);
        dispatcher.Sent[0].Variables["ownerName"].Should().Be("Dona Betinha");

        await using var readDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var stored = await readDb.EmailOutboxes.SingleAsync(e => e.TenantId == tenantId);
        stored.Status.Should().Be("SENT");
        stored.SentAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RunOnceAsync_Com_Falha_De_Entrega_Grava_Erro_E_Agenda_Nova_Tentativa()
    {
        var tenantId = await SeedTenantAsync();
        var recipient = $"owner-{Guid.NewGuid():N}@example.com";

        await SeedPendingEmailAsync(tenantId, recipient, "owner-invite", new Dictionary<string, string>
        {
            ["ownerName"] = "Dona Betinha",
            ["tenantName"] = "Pizzaria Dona Betinha",
            ["token"] = "abc123",
        });

        var dispatcher = new ThrowingEmailDispatcher();
        var worker = BuildWorker(dispatcher, maxAttempts: 5);

        var delivered = await worker.RunOnceAsync(CancellationToken.None);

        delivered.Should().Be(0);

        await using var readDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var stored = await readDb.EmailOutboxes.SingleAsync(e => e.TenantId == tenantId);
        stored.Status.Should().Be("PENDING"); // ainda não esgotou as tentativas.
        stored.Attempts.Should().Be(1);
        stored.LastError.Should().Contain("falha simulada");
        stored.NextAttemptAt.Should().NotBeNull();
    }

    private EmailOutboxDeliveryWorker BuildWorker(IEmailDispatcher dispatcher, int maxAttempts = 5)
    {
        var options = Options.Create(new EmailOutboxOptions
        {
            EncryptionKey = EncryptionKey,
            BatchSize = 20,
            MaxAttempts = maxAttempts,
            RetryDelaySeconds = 60,
        });

        var services = new ServiceCollection();
        services.AddSingleton<IApplicationDbContext>(_ => _fixture.CreateAppDbContext(tenantContext: null));
        services.AddSingleton<IEmailDispatcher>(dispatcher);
        var provider = services.BuildServiceProvider();

        return new EmailOutboxDeliveryWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            options,
            NullLogger<EmailOutboxDeliveryWorker>.Instance);
    }

    private async Task<Guid> SeedTenantAsync()
    {
        var tenantId = Guid.NewGuid();
        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        db.Tenants.Add(Tenant.Create(tenantId, $"tenant-{tenantId:N}", "Tenant de teste"));
        await db.SaveChangesAsync();
        return tenantId;
    }

    private async Task SeedPendingEmailAsync(
        Guid tenantId, string recipient, string template, IReadOnlyDictionary<string, string> variables)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var payloadEncrypted = EmailPayloadCipher.Encrypt(variables, EncryptionKey);
        db.EmailOutboxes.Add(EmailOutbox.Create(tenantId, recipient, template, payloadEncrypted));
        await db.SaveChangesAsync();
    }

    private sealed class RecordingEmailDispatcher : IEmailDispatcher
    {
        public List<(string Recipient, string Template, IReadOnlyDictionary<string, string> Variables)> Sent { get; } = new();

        public Task SendAsync(
            string recipient, string template, IReadOnlyDictionary<string, string> variables, CancellationToken cancellationToken)
        {
            Sent.Add((recipient, template, variables));
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingEmailDispatcher : IEmailDispatcher
    {
        public Task SendAsync(
            string recipient, string template, IReadOnlyDictionary<string, string> variables, CancellationToken cancellationToken)
            => throw new InvalidOperationException("falha simulada de entrega SMTP");
    }
}
