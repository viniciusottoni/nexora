using Nexora.Application.Abstractions.Security;
using Nexora.Application.Platform.SupportAccessTokens;
using Nexora.Application.Tenants.Commands.RecordSupportAccess;
using Nexora.Application.Tenants.Commands.RevokeSupportAccess;
using Nexora.Application.Tenants.Queries.GetSupportAccessHistory;
using Nexora.Domain.Platform;
using Nexora.IntegrationTests.Fakes;
using Nexora.IntegrationTests.Fixtures;
using Nexora.Shared.Errors;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Nexora.IntegrationTests;

/// <summary>
/// US-145 "Acesso de suporte auditado" — ciclo de vida completo contra Postgres real (RLS
/// verdadeiro, ADR-004): concessão gera token utilizável, token expirado/revogado é recusado com
/// código próprio, revogação pelo cliente cessa o acesso imediatamente e — o teste mais importante
/// desta suíte inteira — um token de suporte de um tenant NUNCA valida para outro (RN-015, única
/// exceção autorizada ao isolamento multi-tenant). Complementa <see cref="SupportAccessAuditTests"/>
/// (que cobre só audit_log/EVT-074, herdados de E-09/US-090).
/// </summary>
[Collection("Postgres")]
public sealed class SupportAccessLifecycleTests
{
    private readonly PostgresFixture _fixture;

    public SupportAccessLifecycleTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<Guid> SeedTenantAsync(string label)
    {
        var tenantId = Guid.NewGuid();
        await using var seedDb = _fixture.CreateAppDbContext(tenantContext: null);
        seedDb.Tenants.Add(Tenant.Create(tenantId, $"tenant-{label}-{tenantId:N}", $"Tenant {label}"));
        await seedDb.SaveChangesAsync();
        return tenantId;
    }

    [Fact]
    public async Task Grant_Produz_Token_Utilizavel_Com_Auditoria_E_Evento()
    {
        var tenantId = await SeedTenantAsync("grant");
        var supportUserId = Guid.NewGuid();

        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId: null));
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new RecordSupportAccessCommand(tenantId, supportUserId, "Chamado #482", 60));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Token.Should().NotBeNullOrWhiteSpace();
        result.Value.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);

        var grant = await db.SupportAccesses.SingleAsync(a => a.TenantId == tenantId);
        grant.Reason.Should().Be("Chamado #482");
        grant.DurationMinutes.Should().Be(60);
        grant.IsActive(DateTimeOffset.UtcNow).Should().BeTrue();

        // O token bruto valida de fato pelo validador (mesmo digest que o handler usou).
        var secretDigester = provider.GetRequiredService<ISecretDigester>();
        var validator = new SupportAccessTokenValidator(db, secretDigester);
        var validation = await validator.ValidateAsync(tenantId, result.Value.Token, DateTimeOffset.UtcNow, CancellationToken.None);

        validation.IsSuccess.Should().BeTrue();
        validation.Value!.SupportAccessId.Should().Be(grant.Id);

        var reloaded = await db.SupportAccesses.SingleAsync(a => a.Id == grant.Id);
        reloaded.LastUsedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Token_Expirado_E_Recusado_Com_Codigo_Proprio()
    {
        var tenantId = await SeedTenantAsync("expired");
        var supportUserId = Guid.NewGuid();

        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId: null));
        var sender = provider.GetRequiredService<ISender>();

        // Duração mínima positiva — "expirado" é simulado avançando o relógio de validação, não a
        // duração concedida (a validação do FluentValidation exige DurationMinutes > 0).
        var result = await sender.Send(new RecordSupportAccessCommand(tenantId, supportUserId, "Chamado #1", 1));
        result.IsSuccess.Should().BeTrue();

        var secretDigester = provider.GetRequiredService<ISecretDigester>();
        var validator = new SupportAccessTokenValidator(db, secretDigester);
        var farFuture = result.Value!.ExpiresAt.AddMinutes(1);

        var validation = await validator.ValidateAsync(tenantId, result.Value.Token, farFuture, CancellationToken.None);

        validation.IsFailure.Should().BeTrue();
        validation.Code.Should().Be(ApiErrorCodes.SupportAccessTokenExpired);
    }

    [Fact]
    public async Task Token_Desconhecido_E_Recusado_Com_Codigo_Proprio()
    {
        var tenantId = await SeedTenantAsync("unknown");

        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId: null));
        var secretDigester = provider.GetRequiredService<ISecretDigester>();
        var validator = new SupportAccessTokenValidator(db, secretDigester);

        var validation = await validator.ValidateAsync(tenantId, "token-que-nunca-foi-emitido", DateTimeOffset.UtcNow, CancellationToken.None);

        validation.IsFailure.Should().BeTrue();
        validation.Code.Should().Be(ApiErrorCodes.SupportAccessTokenNotFound);
    }

    [Fact]
    public async Task Revogacao_Pelo_Cliente_Cessa_O_Acesso_Imediatamente()
    {
        var tenantId = await SeedTenantAsync("revoke");
        var supportUserId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();

        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId: null));
        var sender = provider.GetRequiredService<ISender>();

        var grantResult = await sender.Send(new RecordSupportAccessCommand(tenantId, supportUserId, "Chamado #2", 60));
        grantResult.IsSuccess.Should().BeTrue();

        // AsNoTracking: esta suíte reusa `db` em vários passos do teste — sem isso, o identity map
        // do EF Core devolveria esta MESMA instância rastreada (com os valores de quando foi lida,
        // não os do banco) para toda query futura por este Id neste mesmo DbContext, mascarando a
        // revogação feita por outro DbContext/conexão (`tenantDb`) abaixo. Em produção isso nunca
        // acontece porque cada requisição HTTP recebe um DbContext novo (scoped).
        var grant = await db.SupportAccesses.AsNoTracking().SingleAsync(a => a.TenantId == tenantId);

        // A revogação acontece na sessão autenticada do PRÓPRIO tenant — diferente da concessão
        // (ator de plataforma sem tenant), por isso este segundo container usa um
        // StaticTenantContext com o tenant preenchido.
        await using var tenantDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        await using var tenantProvider = MediatRTestContainerFactory.Build(tenantDb, new StaticTenantContext(tenantId));
        var tenantSender = tenantProvider.GetRequiredService<ISender>();

        var revokeResult = await tenantSender.Send(new RevokeSupportAccessCommand(tenantId, grant.Id, ownerUserId));
        revokeResult.IsSuccess.Should().BeTrue();

        // Contexto novo (não `db`) para a validação — mesmo motivo do AsNoTracking acima: precisa
        // enxergar o estado gravado por `tenantDb`, não um valor em cache de identity map.
        await using var validationDb = _fixture.CreateAppDbContext(tenantContext: null);
        var secretDigester = provider.GetRequiredService<ISecretDigester>();
        var validator = new SupportAccessTokenValidator(validationDb, secretDigester);
        var validation = await validator.ValidateAsync(tenantId, grantResult.Value!.Token, DateTimeOffset.UtcNow, CancellationToken.None);

        validation.IsFailure.Should().BeTrue();
        validation.Code.Should().Be(ApiErrorCodes.SupportAccessTokenRevoked);

        var reloaded = await db.SupportAccesses.AsNoTracking().SingleAsync(a => a.Id == grant.Id);
        reloaded.IsRevoked.Should().BeTrue();
        reloaded.RevokedBy.Should().Be(ownerUserId);

        var revokedEvent = await db.DomainEvents.AsNoTracking().SingleAsync(e => e.AggregateId == grant.Id && e.Type == "support.access.revoked");
        revokedEvent.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public async Task Revogar_Id_De_Outro_Tenant_Devolve_SupportAccessNotFound()
    {
        var tenantA = await SeedTenantAsync("cross-a");
        var tenantB = await SeedTenantAsync("cross-b");

        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId: null));
        var sender = provider.GetRequiredService<ISender>();

        var grantResult = await sender.Send(new RecordSupportAccessCommand(tenantA, Guid.NewGuid(), "Chamado #3", 60));
        grantResult.IsSuccess.Should().BeTrue();
        var grant = await db.SupportAccesses.SingleAsync(a => a.TenantId == tenantA);

        await using var tenantBDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantB));
        await using var tenantBProvider = MediatRTestContainerFactory.Build(tenantBDb, new StaticTenantContext(tenantB));
        var tenantBSender = tenantBProvider.GetRequiredService<ISender>();

        var revokeResult = await tenantBSender.Send(new RevokeSupportAccessCommand(tenantB, grant.Id, Guid.NewGuid()));

        revokeResult.IsFailure.Should().BeTrue();
        revokeResult.Code.Should().Be(ApiErrorCodes.SupportAccessNotFound);
    }

    [Fact]
    public async Task Isolamento_Token_De_Um_Tenant_Nunca_Valida_Para_Outro()
    {
        var tenantA = await SeedTenantAsync("iso-a");
        var tenantB = await SeedTenantAsync("iso-b");

        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId: null));
        var sender = provider.GetRequiredService<ISender>();

        var grantResult = await sender.Send(new RecordSupportAccessCommand(tenantA, Guid.NewGuid(), "Chamado #4", 60));
        grantResult.IsSuccess.Should().BeTrue();

        var secretDigester = provider.GetRequiredService<ISecretDigester>();
        var validator = new SupportAccessTokenValidator(db, secretDigester);

        // O MESMO token bruto emitido para o tenant A, pedido contra o tenant B — precisa falhar
        // como "desconhecido" (RLS filtra a linha de A antes mesmo da comparação de hash), nunca
        // suceder nem vazar que o token existe em outro tenant.
        var crossTenantValidation = await validator.ValidateAsync(tenantB, grantResult.Value!.Token, DateTimeOffset.UtcNow, CancellationToken.None);
        crossTenantValidation.IsFailure.Should().BeTrue();
        crossTenantValidation.Code.Should().Be(ApiErrorCodes.SupportAccessTokenNotFound);

        // Controle: o MESMO token continua válido para o tenant correto (A) — prova que a recusa
        // acima é isolamento, não um bug de hash/digest.
        var sameTenantValidation = await validator.ValidateAsync(tenantA, grantResult.Value.Token, DateTimeOffset.UtcNow, CancellationToken.None);
        sameTenantValidation.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Historico_Do_Tenant_Lista_Apenas_As_Proprias_Concessoes()
    {
        var tenantA = await SeedTenantAsync("history-a");
        var tenantB = await SeedTenantAsync("history-b");

        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId: null));
        var sender = provider.GetRequiredService<ISender>();

        await sender.Send(new RecordSupportAccessCommand(tenantA, Guid.NewGuid(), "Chamado #5", 45));
        await sender.Send(new RecordSupportAccessCommand(tenantB, Guid.NewGuid(), "Chamado #6", 45));

        await using var tenantADb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantA));
        await using var tenantAProvider = MediatRTestContainerFactory.Build(tenantADb, new StaticTenantContext(tenantA));
        var tenantASender = tenantAProvider.GetRequiredService<ISender>();

        var history = await tenantASender.Send(new GetSupportAccessHistoryQuery());

        history.IsSuccess.Should().BeTrue();
        history.Value!.Data.Should().ContainSingle();
        history.Value.Data[0].TenantId.Should().Be(tenantA);
        history.Value.Data[0].Reason.Should().Be("Chamado #5");
        history.Value.Data[0].IsActive.Should().BeTrue();
    }
}
