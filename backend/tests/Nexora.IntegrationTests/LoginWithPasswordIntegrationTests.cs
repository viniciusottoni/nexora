using Nexora.Application.Abstractions.Security;
using Nexora.Application.Auth.Commands.LoginWithPassword;
using Nexora.Domain.Platform;
using Nexora.IntegrationTests.Fakes;
using Nexora.IntegrationTests.Fixtures;
using Nexora.Shared.Errors;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.IdentityModel.Tokens.Jwt;
using Xunit;

namespace Nexora.IntegrationTests;

/// <summary>
/// Cobre o gap encontrado tarde na auditoria de US-001 e nunca corrigido pelas correções de
/// US-004/plumbing: <c>LoginWithPasswordCommandHandler</c> depende de <c>auth_lookup_user(citext)</c>
/// para achar o tenant de um e-mail ANTES de <c>app.tenant_id</c> existir (RLS nega leitura de
/// <c>app_user</c> sem contexto) — sem a função no banco, TODO login por senha falhava em runtime,
/// apesar do build compilar e dos outros testes de Auth (que usam PIN, não senha) passarem. A
/// migration <c>CreateAuthLookupUserFunction</c> cria a função; este teste prova o fluxo completo
/// contra Postgres real (Testcontainers), não apenas o SQL isolado.
/// </summary>
[Collection("Postgres")]
public sealed class LoginWithPasswordIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public LoginWithPasswordIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Login_Com_Credenciais_Validas_Emite_Tokens_E_Registra_Evento()
    {
        var tenantId = Guid.NewGuid();
        const string email = "gestor@dona-betinha.test";
        const string password = "senha-forte-de-teste-123";

        await using var seedDb = _fixture.CreateAppDbContext(tenantContext: null);
        var hasher = new Argon2CredentialHasherForTest();

        seedDb.Tenants.Add(Tenant.Create(tenantId, $"tenant-{tenantId:N}", "Pizzaria de teste"));
        await seedDb.SaveChangesAsync();

        await using var seedTenantDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        seedTenantDb.Stores.Add(Store.Create(tenantId, "Matriz", isDefault: true));

        var role = Role.Create(tenantId, "OWNER", "Proprietário", isSystem: true);
        role.UpdatePermissions("[\"*\"]");
        seedTenantDb.Roles.Add(role);

        var user = AppUser.Create(tenantId, "Gestora de teste", email, hasher.Hash(password), pinHash: null);
        seedTenantDb.Users.Add(user);
        seedTenantDb.UserRoles.Add(UserRole.Create(tenantId, user.Id, role.Id));

        await seedTenantDb.SaveChangesAsync();

        // Sem contexto de tenant, como uma requisição de login real chega antes de saber o tenant —
        // é exatamente esse caminho (RLS negando app_user sem app.tenant_id) que auth_lookup_user()
        // precisa contornar.
        await using var loginDb = _fixture.CreateAppDbContext(tenantContext: null);
        using var provider = MediatRTestContainerFactory.Build(loginDb, new StaticTenantContext(Guid.Empty));
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new LoginWithPasswordCommand(email, password, Otp: null));

        result.IsSuccess.Should().BeTrue(result.IsFailure ? result.Code : string.Empty);
        result.Value!.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Value.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.Value.Tenant.Id.Should().Be(tenantId);

        await using var assertDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var events = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
            assertDb.DomainEvents.Where(e => e.Type == "user.authenticated"));
        events.Should().ContainSingle();

        var tokenSessionId = Guid.Parse(
            new JwtSecurityTokenHandler()
                .ReadJwtToken(result.Value.AccessToken)
                .Claims.Single(claim => claim.Type == "ses")
                .Value);
        var persistedSession = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.SingleAsync(
            assertDb.AuthSessions.Where(session => session.UserId == user.Id));
        persistedSession.Id.Should().Be(
            tokenSessionId,
            "a sessão referenciada pelo JWT precisa ser exatamente a sessão salva no banco");
    }

    [Fact]
    public async Task Login_Com_Senha_Errada_Falha_Sem_Revelar_Se_O_Email_Existe()
    {
        var tenantId = Guid.NewGuid();
        const string email = "outra-gestora@dona-betinha.test";

        await using var seedDb = _fixture.CreateAppDbContext(tenantContext: null);
        seedDb.Tenants.Add(Tenant.Create(tenantId, $"tenant-{tenantId:N}", "Pizzaria de teste 2"));
        await seedDb.SaveChangesAsync();

        await using var seedTenantDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var hasher = new Argon2CredentialHasherForTest();
        seedTenantDb.Users.Add(AppUser.Create(tenantId, "Gestora 2", email, hasher.Hash("senha-correta"), pinHash: null));
        await seedTenantDb.SaveChangesAsync();

        await using var loginDb = _fixture.CreateAppDbContext(tenantContext: null);
        using var provider = MediatRTestContainerFactory.Build(loginDb, new StaticTenantContext(Guid.Empty));
        var mediator = provider.GetRequiredService<IMediator>();

        var wrongPassword = await mediator.Send(new LoginWithPasswordCommand(email, "senha-errada", Otp: null));
        var unknownEmail = await mediator.Send(new LoginWithPasswordCommand("ninguem@dona-betinha.test", "qualquer", Otp: null));

        wrongPassword.IsFailure.Should().BeTrue();
        wrongPassword.Code.Should().Be(ApiErrorCodes.AuthInvalidCredentials);
        unknownEmail.IsFailure.Should().BeTrue();
        unknownEmail.Code.Should().Be(wrongPassword.Code, "mesma mensagem para senha errada e e-mail inexistente evita oráculo de enumeração");
    }

    /// <summary>Wrapper mínimo — evita puxar <c>Nexora.Infrastructure</c> só para hash em setup de teste.</summary>
    private sealed class Argon2CredentialHasherForTest : ICredentialHasher
    {
        private readonly ICredentialHasher _inner = new Nexora.Infrastructure.Auth.Argon2CredentialHasher();
        public string Hash(string plainText) => _inner.Hash(plainText);
        public bool Verify(string hash, string plainText) => _inner.Verify(hash, plainText);
    }
}
