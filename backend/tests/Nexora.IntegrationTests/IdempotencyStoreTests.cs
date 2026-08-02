using Nexora.Application.Abstractions.Idempotency;
using Nexora.Infrastructure.Idempotency;
using Nexora.IntegrationTests.Fixtures;
using FluentAssertions;
using Xunit;

namespace Nexora.IntegrationTests;

/// <summary>
/// Prova <see cref="IdempotencyStore"/> (ADR-020) contra um PostgreSQL real (Testcontainers), com
/// as migrations reais aplicadas — inclusive <c>AdjustIdempotencyKeyTenantScope</c>, que desliga
/// RLS em <c>idempotency_key</c> e torna <c>tenant_id</c> anulável (ver comentário completo em
/// <c>Nexora.Domain.Platform.IdempotencyKey</c>). Conectado como <c>app_user_role</c> (o mesmo
/// papel de runtime sem <c>BYPASSRLS</c>/<c>DELETE</c> usado pelas Apis) — não pelo superusuário
/// do container — para que qualquer regressão nesses dois pontos (RLS ligado de novo, ou um
/// <c>DELETE</c> físico introduzido por engano) apareça aqui como falha real de permissão, do
/// mesmo jeito que apareceria em produção.
/// </summary>
/// <remarks>
/// Cobre diretamente o mecanismo de armazenamento por trás do <c>IdempotencyMiddleware</c>
/// (duplicado em Api.Edge/Api.Cloud, que não referenciam Nexora.IntegrationTests) — os cenários
/// "Como validar" do ADR-020 (mesma chave duas vezes -&gt; uma execução; concorrência -&gt; 409;
/// reenvio após falha -&gt; nova tentativa real) são provados aqui no nível do store; o
/// comportamento do middleware em si (leitura do header, resposta 422/409, replay do corpo) é
/// coberto por <c>Nexora.UnitTests.Idempotency.IdempotencyMiddlewareTests</c> com um duplo de
/// <see cref="IIdempotencyStore"/> — a combinação prova a mesma coisa que um teste HTTP de ponta a
/// ponta provaria, sem a infraestrutura adicional de JWT/tenant bootstrap que um
/// <c>WebApplicationFactory</c> completo exigiria (ver relatório da tarefa).
/// </remarks>
[Collection("Postgres")]
public sealed class IdempotencyStoreTests
{
    private readonly PostgresFixture _fixture;

    public IdempotencyStoreTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task BeginAsync_Primeira_Chamada_Reserva_A_Chave()
    {
        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        var store = new IdempotencyStore(db);
        var key = Guid.NewGuid().ToString("N");

        var outcome = await store.BeginAsync(
            key, tenantId: null, "POST /v1/devices/pair", "hash-1", DateTimeOffset.UtcNow.AddHours(24), CancellationToken.None);

        outcome.Should().Be(IdempotencyBeginOutcome.Started);

        var record = await store.FindAsync(key, CancellationToken.None);
        record.Should().NotBeNull();
        record!.Status.Should().Be("IN_PROGRESS");
        record.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task BeginAsync_Com_Tenant_Nulo_Funciona_Sem_RLS_Bloquear_A_Escrita()
    {
        // Prova a própria razão de existir da migration AdjustIdempotencyKeyTenantScope: uma
        // rota sem tenant resolvido (pareamento de dispositivo, provisionamento de tenant,
        // registro de instalação) precisa conseguir gravar aqui. Antes da migration, RLS
        // recusaria (WITH CHECK "tenant_id = current_tenant_id()" nunca bate com NULL).
        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        var store = new IdempotencyStore(db);
        var key = Guid.NewGuid().ToString("N");

        var outcome = await store.BeginAsync(
            key, tenantId: null, "POST /v1/platform/tenants", "hash-provision", DateTimeOffset.UtcNow.AddHours(24), CancellationToken.None);

        outcome.Should().Be(IdempotencyBeginOutcome.Started);
    }

    [Fact]
    public async Task Segunda_Chamada_Concorrente_Com_A_Mesma_Chave_E_Rejeitada_Enquanto_Em_Processamento()
    {
        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        var store = new IdempotencyStore(db);
        var key = Guid.NewGuid().ToString("N");

        var first = await store.BeginAsync(
            key, tenantId: null, "POST /v1/devices/pair", "hash-1", DateTimeOffset.UtcNow.AddHours(24), CancellationToken.None);
        var second = await store.BeginAsync(
            key, tenantId: null, "POST /v1/devices/pair", "hash-1", DateTimeOffset.UtcNow.AddHours(24), CancellationToken.None);

        first.Should().Be(IdempotencyBeginOutcome.Started);
        // ADR-020: "chave em processamento (concorrência) -> 409, cliente reenvia com backoff" —
        // o middleware traduz este outcome em 409 REQUEST_IN_PROGRESS.
        second.Should().Be(IdempotencyBeginOutcome.AlreadyReserved);
    }

    [Fact]
    public async Task CompleteAsync_Grava_A_Resposta_E_Reserva_Subsequente_Continua_Bloqueada()
    {
        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        var store = new IdempotencyStore(db);
        var key = Guid.NewGuid().ToString("N");

        await store.BeginAsync(key, tenantId: null, "POST /v1/roles", "hash-1", DateTimeOffset.UtcNow.AddHours(24), CancellationToken.None);
        await store.CompleteAsync(key, responseStatus: 201, responseBody: """{"id":"abc"}""", CancellationToken.None);

        var record = await store.FindAsync(key, CancellationToken.None);
        record.Should().NotBeNull();
        record!.IsCompleted.Should().BeTrue();
        record.ResponseStatus.Should().Be(201);
        // Comparação semântica, não byte a byte: a coluna é jsonb (ADR-020), e o Postgres
        // reformata o texto ao devolvê-lo (ex.: espaço depois de ":") — semanticamente idêntico,
        // é exatamente isso que o middleware devolve no reenvio (JSON válido, não os bytes
        // originais literais).
        System.Text.Json.Nodes.JsonNode.Parse(record.ResponseBody!)!.ToJsonString()
            .Should().Be(System.Text.Json.Nodes.JsonNode.Parse("""{"id":"abc"}""")!.ToJsonString());

        // Reenviar com a mesma chave depois de completa continua "reservado" do ponto de vista de
        // BeginAsync — é o middleware (via FindAsync, chamado ANTES de BeginAsync) quem decide
        // devolver a resposta gravada em vez de tentar reservar de novo.
        var again = await store.BeginAsync(
            key, tenantId: null, "POST /v1/roles", "hash-1", DateTimeOffset.UtcNow.AddHours(24), CancellationToken.None);
        again.Should().Be(IdempotencyBeginOutcome.AlreadyReserved);
    }

    [Fact]
    public async Task DiscardAsync_Libera_A_Chave_Para_Uma_Tentativa_Real_Depois_De_Uma_Falha()
    {
        // ADR-020: "requisição original falhou com 5xx -> não armazena, permite nova tentativa
        // real" — sem privilégio de DELETE (Docs/Domain/10 §4), o "abandono" é um UPDATE para
        // status=FAILED, que o INSERT ... ON CONFLICT DO UPDATE de BeginAsync reconhece como
        // livre para reclamar.
        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        var store = new IdempotencyStore(db);
        var key = Guid.NewGuid().ToString("N");

        await store.BeginAsync(key, tenantId: null, "POST /v1/roles", "hash-1", DateTimeOffset.UtcNow.AddHours(24), CancellationToken.None);
        await store.DiscardAsync(key, CancellationToken.None);

        var retry = await store.BeginAsync(
            key, tenantId: null, "POST /v1/roles", "hash-1", DateTimeOffset.UtcNow.AddHours(24), CancellationToken.None);

        retry.Should().Be(IdempotencyBeginOutcome.Started);
    }

    [Fact]
    public async Task Chave_Expirada_E_Reclamada_Como_Requisicao_Nova()
    {
        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        var store = new IdempotencyStore(db);
        var key = Guid.NewGuid().ToString("N");

        // Expira no passado de propósito -> ADR-020 "chave expirada (> 24h) -> trata como
        // requisição nova".
        await store.BeginAsync(key, tenantId: null, "POST /v1/roles", "hash-1", DateTimeOffset.UtcNow.AddSeconds(-1), CancellationToken.None);
        await store.CompleteAsync(key, 201, """{"id":"abc"}""", CancellationToken.None);

        var retry = await store.BeginAsync(
            key, tenantId: null, "POST /v1/roles", "hash-2", DateTimeOffset.UtcNow.AddHours(24), CancellationToken.None);

        retry.Should().Be(IdempotencyBeginOutcome.Started);

        var record = await store.FindAsync(key, CancellationToken.None);
        record!.RequestHash.Should().Be("hash-2");
        record.IsCompleted.Should().BeFalse();
    }
}
