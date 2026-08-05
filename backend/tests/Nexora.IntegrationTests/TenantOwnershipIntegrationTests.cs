using Nexora.Application.Tenants.Commands.ReissueOwnerInvite;
using Nexora.Application.Tenants.Commands.RevokeOwnerInvite;
using Nexora.Application.Tenants.Commands.TransferTenantOwnership;
using Nexora.Application.Tenants.Commands.UnlockOwnerAccess;
using Nexora.Application.Tenants.Queries.GetTenantOwnership;
using Nexora.Contracts.Tenants;
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
/// US-155 "Proprietários, usuários iniciais e convites" — prova os quatro cenários Gherkin da US
/// ("Convite expirado", "E-mail corrigido antes da aceitação", "Transferência de titularidade",
/// "Segredo não recuperável") de ponta a ponta pelo pipeline MediatR real, contra Postgres real com
/// RLS ligado (<see cref="PostgresFixture"/>, mesma infraestrutura de
/// <c>GetTenantOverviewIntegrationTests</c>), mais isolamento cross-tenant (RN-015) e a garantia
/// transacional de "exatamente um proprietário" sob concorrência real.
/// </summary>
[Collection("Postgres")]
public sealed class TenantOwnershipIntegrationTests
{
    private readonly PostgresFixture _fixture;

    public TenantOwnershipIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>Cenário Gherkin: "Convite expirado".</summary>
    [Fact]
    public async Task Reenvio_De_Convite_Expirado_Cria_Novo_Token_Com_72h_E_Invalida_O_Anterior()
    {
        var marker = UniqueMarker();
        var tenantId = await SeedTenantAsync($"Pizzaria {marker}");
        var ownerRoleId = await SeedOwnerRoleAsync(tenantId);
        var ownerEmail = $"dono-{marker}@example.com";
        var (ownerId, expiredInviteId) = await SeedInvitedOwnerAsync(
            tenantId, ownerRoleId, "Dona Betinha", ownerEmail, expiresAt: DateTimeOffset.UtcNow.AddHours(-1));

        var beforeReissue = DateTimeOffset.UtcNow;
        var result = await SendAsync(new ReissueOwnerInviteCommand(tenantId, "Dona Betinha", ownerEmail, "Convite expirado — reenvio solicitado", null));

        result.IsSuccess.Should().BeTrue();
        var response = result.Value!;
        response.SentTo.Should().Be(ownerEmail);
        response.ExpiresAt.Should().BeCloseTo(beforeReissue.AddHours(72), TimeSpan.FromMinutes(1));
        response.InviteId.Should().NotBe(expiredInviteId);

        await using var readDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));

        var oldInvite = await readDb.OwnerInvites.SingleAsync(i => i.Id == expiredInviteId);
        oldInvite.IsRevoked.Should().BeTrue("qualquer token anterior deve ser invalidado");
        oldInvite.RevokedReason.Should().NotBeNullOrWhiteSpace();

        var newInvite = await readDb.OwnerInvites.SingleAsync(i => i.Id == response.InviteId);
        newInvite.IsRevoked.Should().BeFalse();
        newInvite.IsConsumed.Should().BeFalse();
        newInvite.UserId.Should().Be(ownerId);

        // "Apenas o novo convite deve poder ser aceito" — prova indireta pela máquina de estados:
        // ResolveStatus do antigo é REVOKED (nunca aceitável), do novo é PENDING.
        var now = DateTimeOffset.UtcNow;
        oldInvite.ResolveStatus(now).Should().Be("REVOKED");
        newInvite.ResolveStatus(now).Should().Be("PENDING");
    }

    /// <summary>Cenário Gherkin: "E-mail corrigido antes da aceitação".</summary>
    [Fact]
    public async Task Correcao_De_Email_Revoga_O_Convite_Anterior_E_Envia_Um_Novo_Ao_Endereco_Corrigido()
    {
        var marker = UniqueMarker();
        var tenantId = await SeedTenantAsync($"Hamburgueria {marker}");
        var ownerRoleId = await SeedOwnerRoleAsync(tenantId);
        var wrongEmail = $"errado-{marker}@example.com";
        var correctEmail = $"correto-{marker}@example.com";
        var (ownerId, oldInviteId) = await SeedInvitedOwnerAsync(
            tenantId, ownerRoleId, "Seu Zé", wrongEmail, expiresAt: DateTimeOffset.UtcNow.AddHours(70));

        var result = await SendAsync(new ReissueOwnerInviteCommand(
            tenantId, "Seu Zé", correctEmail, "Correção solicitada no chamado #91", null));

        result.IsSuccess.Should().BeTrue();
        result.Value!.SentTo.Should().Be(correctEmail);

        await using var readDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));

        (await readDb.OwnerInvites.SingleAsync(i => i.Id == oldInviteId)).IsRevoked.Should().BeTrue();

        var owner = await readDb.Users.SingleAsync(u => u.Id == ownerId);
        owner.Email.Should().Be(correctEmail);

        var tenant = await readDb.Tenants.AsNoTracking().SingleAsync(t => t.Id == tenantId);
        tenant.OwnerEmail.Should().Be(correctEmail);

        var outboxEntry = await readDb.EmailOutboxes.SingleAsync(e => e.Recipient == correctEmail);
        outboxEntry.Status.Should().Be("PENDING");
    }

    [Fact]
    public async Task Correcao_De_Email_Ja_Usado_Por_Outro_Usuario_E_Recusada()
    {
        var marker = UniqueMarker();

        var otherTenantId = await SeedTenantAsync($"Outro estabelecimento {marker}");
        var takenEmail = $"ja-usado-{marker}@example.com";
        await using (var db = _fixture.CreateAppDbContext(new StaticTenantContext(otherTenantId)))
        {
            db.Users.Add(AppUser.Create(otherTenantId, "Outro Usuário", takenEmail, passwordHash: "hash-teste", pinHash: null));
            await db.SaveChangesAsync();
        }

        var tenantId = await SeedTenantAsync($"Pizzaria {marker}");
        var ownerRoleId = await SeedOwnerRoleAsync(tenantId);
        var (_, _) = await SeedInvitedOwnerAsync(tenantId, ownerRoleId, "Dona Betinha", $"dono-{marker}@example.com", DateTimeOffset.UtcNow.AddHours(70));

        var result = await SendAsync(new ReissueOwnerInviteCommand(tenantId, "Dona Betinha", takenEmail, "Correção", null));

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.OwnershipEmailAlreadyInUse);
    }

    [Fact]
    public async Task Revogacao_Explicita_Marca_Convite_Pendente_Como_Revogado()
    {
        var marker = UniqueMarker();
        var tenantId = await SeedTenantAsync($"Pizzaria {marker}");
        var ownerRoleId = await SeedOwnerRoleAsync(tenantId);
        var (_, inviteId) = await SeedInvitedOwnerAsync(tenantId, ownerRoleId, "Dona Betinha", $"dono-{marker}@example.com", DateTimeOffset.UtcNow.AddHours(70));

        var result = await SendAsync(new RevokeOwnerInviteCommand(tenantId, inviteId, "Convite não é mais necessário", null));

        result.IsSuccess.Should().BeTrue();

        await using var readDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        (await readDb.OwnerInvites.SingleAsync(i => i.Id == inviteId)).IsRevoked.Should().BeTrue();
    }

    /// <summary>RN-015 — convite de OUTRO tenant nunca é encontrado pelo id, mesmo mandando o id certo (404-equivalente, nunca 403).</summary>
    [Fact]
    public async Task Revogar_Convite_De_Outro_Tenant_Retorna_Convite_Nao_Encontrado()
    {
        var marker = UniqueMarker();
        var tenantA = await SeedTenantAsync($"Tenant A {marker}");
        var tenantB = await SeedTenantAsync($"Tenant B {marker}");
        var roleB = await SeedOwnerRoleAsync(tenantB);
        var (_, inviteIdOfB) = await SeedInvitedOwnerAsync(tenantB, roleB, "Dono B", $"donob-{marker}@example.com", DateTimeOffset.UtcNow.AddHours(70));

        var result = await SendAsync(new RevokeOwnerInviteCommand(tenantA, inviteIdOfB, "Tentativa cross-tenant", null));

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.OwnershipInviteNotFound);
    }

    /// <summary>
    /// Cenário Gherkin: "Transferência de titularidade". A linha de <c>user_role</c> do papel OWNER
    /// é REPOSTA para o novo dono (<see cref="UserRole.TransferTo"/>, um UPDATE) em vez de excluída
    /// e recriada — o papel de runtime da aplicação (<c>app_user_role</c>) não tem <c>GRANT DELETE</c>
    /// (ver docstring de <c>TransferTenantOwnershipCommandHandler</c>). Um papel PRÉ-EXISTENTE do
    /// dono anterior (CASHIER, atribuído antes dele virar dono) NÃO é tocado por
    /// <c>keepPreviousAsAdmin=false</c> — só o papel OWNER em si é revogado do anterior, que é o que
    /// o cenário pede ("não deve manter privilégios [de proprietário] por acidente").
    /// </summary>
    [Fact]
    public async Task Transferencia_De_Titularidade_Deixa_Exatamente_Um_Dono_E_Revoga_O_Papel_Owner_Do_Anterior()
    {
        var marker = UniqueMarker();
        var tenantId = await SeedTenantAsync($"Pizzaria {marker}");
        var ownerRoleId = await SeedOwnerRoleAsync(tenantId);
        var cashierRoleId = await SeedRoleAsync(tenantId, "CASHIER", "Caixa");
        var previousOwnerId = await SeedActiveOwnerAsync(tenantId, ownerRoleId, "Dona Betinha", $"antigo-{marker}@example.com");
        var newOwnerId = await SeedActiveUserAsync(tenantId, "Novo Dono", $"novo-{marker}@example.com");

        // Papel extra do dono anterior, PRÉ-EXISTENTE — prova que keepPreviousAsAdmin=false só
        // revoga o papel OWNER, nunca papéis que o usuário já tinha antes de virar dono.
        await using (var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId)))
        {
            db.UserRoles.Add(UserRole.Create(tenantId, previousOwnerId, cashierRoleId));
            await db.SaveChangesAsync();
        }

        var result = await SendAsync(new TransferTenantOwnershipCommand(tenantId, newOwnerId, "Alteração societária", KeepPreviousAsAdmin: false, null));

        result.IsSuccess.Should().BeTrue();
        result.Value!.PreviousOwnerUserId.Should().Be(previousOwnerId);
        result.Value!.NewOwnerUserId.Should().Be(newOwnerId);

        await using var readDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));

        var ownerAssignments = await readDb.UserRoles.Where(ur => ur.RoleId == ownerRoleId).ToListAsync();
        ownerAssignments.Should().ContainSingle().Which.UserId.Should().Be(newOwnerId);

        var previousOwnerRoles = await readDb.UserRoles.Where(ur => ur.UserId == previousOwnerId).ToListAsync();
        previousOwnerRoles.Should().NotContain(ur => ur.RoleId == ownerRoleId, "o anterior não deve manter o papel de proprietário por acidente");
        previousOwnerRoles.Should().ContainSingle(ur => ur.RoleId == cashierRoleId, "papel pré-existente, não relacionado à titularidade, permanece intacto");

        var tenant = await readDb.Tenants.AsNoTracking().SingleAsync(t => t.Id == tenantId);
        tenant.OwnerEmail.Should().Be($"novo-{marker}@example.com");

        var transferHistory = await readDb.OwnershipTransfers.SingleAsync(t => t.TenantId == tenantId);
        transferHistory.PreviousOwnerUserId.Should().Be(previousOwnerId);
        transferHistory.NewOwnerUserId.Should().Be(newOwnerId);
        transferHistory.PreviousKeptAsAdmin.Should().BeFalse();
    }

    [Fact]
    public async Task Transferencia_Com_KeepPreviousAsAdmin_Mantem_Papel_Equivalente_Existente()
    {
        var marker = UniqueMarker();
        var tenantId = await SeedTenantAsync($"Restaurante {marker}");
        var ownerRoleId = await SeedOwnerRoleAsync(tenantId);
        var managerRoleId = await SeedRoleAsync(tenantId, "MANAGER", "Gerente");
        var previousOwnerId = await SeedActiveOwnerAsync(tenantId, ownerRoleId, "Dona Betinha", $"antigo-{marker}@example.com");
        var newOwnerId = await SeedActiveUserAsync(tenantId, "Novo Dono", $"novo-{marker}@example.com");

        var result = await SendAsync(new TransferTenantOwnershipCommand(tenantId, newOwnerId, "Transição planejada", KeepPreviousAsAdmin: true, null));

        result.IsSuccess.Should().BeTrue();

        await using var readDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var previousOwnerRoles = await readDb.UserRoles.Where(ur => ur.UserId == previousOwnerId).ToListAsync();
        previousOwnerRoles.Should().ContainSingle().Which.RoleId.Should().Be(managerRoleId);
    }

    [Fact]
    public async Task Transferencia_Para_O_Mesmo_Dono_E_Recusada()
    {
        var marker = UniqueMarker();
        var tenantId = await SeedTenantAsync($"Pizzaria {marker}");
        var ownerRoleId = await SeedOwnerRoleAsync(tenantId);
        var ownerId = await SeedActiveOwnerAsync(tenantId, ownerRoleId, "Dona Betinha", $"dono-{marker}@example.com");

        var result = await SendAsync(new TransferTenantOwnershipCommand(tenantId, ownerId, "Motivo qualquer", KeepPreviousAsAdmin: false, null));

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.OwnershipSameOwner);
    }

    /// <summary>RN-015 — usuário-alvo de OUTRO tenant nunca é encontrado, mesmo com o id certo (404-equivalente).</summary>
    [Fact]
    public async Task Transferencia_Para_Usuario_De_Outro_Tenant_Retorna_Usuario_Nao_Encontrado()
    {
        var marker = UniqueMarker();
        var tenantA = await SeedTenantAsync($"Tenant A {marker}");
        var ownerRoleA = await SeedOwnerRoleAsync(tenantA);
        await SeedActiveOwnerAsync(tenantA, ownerRoleA, "Dono A", $"donoa-{marker}@example.com");

        var tenantB = await SeedTenantAsync($"Tenant B {marker}");
        var userOfB = await SeedActiveUserAsync(tenantB, "Usuário B", $"usuariob-{marker}@example.com");

        var result = await SendAsync(new TransferTenantOwnershipCommand(tenantA, userOfB, "Tentativa cross-tenant", KeepPreviousAsAdmin: false, null));

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.OwnershipTargetUserNotFound);
    }

    /// <summary>
    /// Concorrência real: duas transferências simultâneas do MESMO tenant (para alvos diferentes)
    /// nunca podem coexistir com dois donos — mecanismo escolhido é <c>SELECT ... FOR UPDATE</c> na
    /// linha do papel OWNER (<see cref="Nexora.Application.Abstractions.Persistence.IApplicationDbContext.LockOwnerRoleIdAsync"/>),
    /// dentro da MESMA transação que o <c>TransactionBehavior</c> já abre por comando — serializa as
    /// duas ao invés de rejeitar uma. Ver docstring de <c>TransferTenantOwnershipCommandHandler</c>.
    /// </summary>
    [Fact]
    public async Task Duas_Transferencias_Concorrentes_Do_Mesmo_Tenant_Nunca_Deixam_Dois_Donos()
    {
        var marker = UniqueMarker();
        var tenantId = await SeedTenantAsync($"Pizzaria concorrente {marker}");
        var ownerRoleId = await SeedOwnerRoleAsync(tenantId);
        await SeedActiveOwnerAsync(tenantId, ownerRoleId, "Dona Betinha", $"antigo-{marker}@example.com");
        var candidateOne = await SeedActiveUserAsync(tenantId, "Candidato 1", $"candidato1-{marker}@example.com");
        var candidateTwo = await SeedActiveUserAsync(tenantId, "Candidato 2", $"candidato2-{marker}@example.com");

        var taskOne = Task.Run(() => SendAsync(new TransferTenantOwnershipCommand(tenantId, candidateOne, "Concorrência 1", false, null)));
        var taskTwo = Task.Run(() => SendAsync(new TransferTenantOwnershipCommand(tenantId, candidateTwo, "Concorrência 2", false, null)));

        var results = await Task.WhenAll(taskOne, taskTwo);

        // Nenhuma das duas deve travar em deadlock ou explodir com exceção não tratada — a trava
        // pessimista SERIALIZA (a segunda só roda depois da primeira commitar), não rejeita.
        results.Should().OnlyContain(r => r.IsSuccess);

        await using var readDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var finalOwners = await readDb.UserRoles.Where(ur => ur.RoleId == ownerRoleId).ToListAsync();

        finalOwners.Should().ContainSingle("nunca pode existir mais de um proprietário principal, mesmo sob concorrência real");
        new[] { candidateOne, candidateTwo }.Should().Contain(finalOwners[0].UserId);
    }

    [Fact]
    public async Task Desbloqueio_Administrativo_Reativa_Sem_Tocar_Na_Senha()
    {
        var marker = UniqueMarker();
        var tenantId = await SeedTenantAsync($"Pizzaria {marker}");
        var ownerRoleId = await SeedOwnerRoleAsync(tenantId);
        var ownerId = await SeedActiveOwnerAsync(tenantId, ownerRoleId, "Dona Betinha", $"dono-{marker}@example.com");

        string? passwordHashBefore;
        await using (var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId)))
        {
            var ownerBeforeUnlock = await db.Users.SingleAsync(u => u.Id == ownerId);
            ownerBeforeUnlock.Block(DateTimeOffset.UtcNow.AddHours(1));
            passwordHashBefore = ownerBeforeUnlock.PasswordHash;
            await db.SaveChangesAsync();
        }

        var result = await SendAsync(new UnlockOwnerAccessCommand(tenantId, "Chamado de suporte #12 — bloqueio incorreto", null));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be("ACTIVE");

        await using var readDb = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var ownerAfterUnlock = await readDb.Users.SingleAsync(u => u.Id == ownerId);
        ownerAfterUnlock.Status.Should().Be(UserStatus.Active);
        ownerAfterUnlock.BlockedUntil.Should().BeNull();
        ownerAfterUnlock.PasswordHash.Should().Be(passwordHashBefore, "desbloqueio nunca define nem altera a senha");
    }

    [Fact]
    public async Task Desbloqueio_De_Proprietario_Nao_Bloqueado_E_Recusado()
    {
        var marker = UniqueMarker();
        var tenantId = await SeedTenantAsync($"Pizzaria {marker}");
        var ownerRoleId = await SeedOwnerRoleAsync(tenantId);
        await SeedActiveOwnerAsync(tenantId, ownerRoleId, "Dona Betinha", $"dono-{marker}@example.com");

        var result = await SendAsync(new UnlockOwnerAccessCommand(tenantId, "Motivo", null));

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.OwnershipOwnerNotBlocked);
    }

    /// <summary>Cenário Gherkin: "Segredo não recuperável" — o histórico nunca devolve hash/token, e o e-mail de entrega é rastreável via email_outbox.</summary>
    [Fact]
    public async Task Consulta_De_Ownership_Nunca_Devolve_Segredo_E_Reporta_Entrega_Do_Email_Outbox()
    {
        var marker = UniqueMarker();
        var tenantId = await SeedTenantAsync($"Pizzaria {marker}");
        var ownerRoleId = await SeedOwnerRoleAsync(tenantId);
        var ownerEmail = $"dono-{marker}@example.com";
        await SeedInvitedOwnerAsync(tenantId, ownerRoleId, "Dona Betinha", ownerEmail, DateTimeOffset.UtcNow.AddHours(70));

        // Convite original (provisionamento, sem EmailOutboxId) reporta entrega "UNKNOWN".
        var queryResult = await SendQueryAsync(tenantId);

        queryResult.IsSuccess.Should().BeTrue();
        var value = queryResult.Value!;
        value.Owner.Status.Should().Be("INVITED");
        value.Invites.Should().ContainSingle().Which.DeliveryStatus.Should().Be("UNKNOWN");

        // Reenvio cria um convite NOVO correlacionado a um email_outbox real -> "PENDING".
        await SendAsync(new ReissueOwnerInviteCommand(tenantId, "Dona Betinha", ownerEmail, "Reenvio", null));

        var afterReissue = await SendQueryAsync(tenantId);
        var reissued = afterReissue.Value!.Invites.Should().Contain(i => i.Status == "PENDING").Subject;
        reissued.DeliveryStatus.Should().Be("PENDING");

        var json = System.Text.Json.JsonSerializer.Serialize(afterReissue.Value);
        json.ToLowerInvariant().Should().NotContain("hash");
        json.ToLowerInvariant().Should().NotContain("secret");
    }

    private static string UniqueMarker() => $"qa{Guid.NewGuid():N}"[..14];

    private async Task<Guid> SeedTenantAsync(string name)
    {
        var tenantId = Guid.NewGuid();
        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        db.Tenants.Add(Tenant.Create(tenantId, $"tenant-{tenantId:N}", name));
        await db.SaveChangesAsync();
        return tenantId;
    }

    private async Task<Guid> SeedOwnerRoleAsync(Guid tenantId) => await SeedRoleAsync(tenantId, "OWNER", "Proprietário");

    private async Task<Guid> SeedRoleAsync(Guid tenantId, string code, string name)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var role = Role.Create(tenantId, code, name, isSystem: true);
        role.UpdatePermissions("[\"manage_tenant\"]");
        db.Roles.Add(role);
        await db.SaveChangesAsync();
        return role.Id;
    }

    private async Task<(Guid OwnerId, Guid InviteId)> SeedInvitedOwnerAsync(
        Guid tenantId, Guid roleId, string name, string email, DateTimeOffset expiresAt)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var user = AppUser.Invite(tenantId, name, email);
        db.Users.Add(user);
        db.UserRoles.Add(UserRole.Create(tenantId, user.Id, roleId));

        var invite = OwnerInvite.Create(tenantId, user.Id, email, secretHash: $"secret-hash-{Guid.NewGuid():N}", expiresAt: expiresAt);
        db.OwnerInvites.Add(invite);
        await db.SaveChangesAsync();

        return (user.Id, invite.Id);
    }

    private async Task<Guid> SeedActiveOwnerAsync(Guid tenantId, Guid roleId, string name, string email)
    {
        var userId = await SeedActiveUserAsync(tenantId, name, email);
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        db.UserRoles.Add(UserRole.Create(tenantId, userId, roleId));
        await db.SaveChangesAsync();
        return userId;
    }

    private async Task<Guid> SeedActiveUserAsync(Guid tenantId, string name, string email)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));
        var user = AppUser.Create(tenantId, name, email, passwordHash: "hash-de-teste-nao-e-producao", pinHash: null);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    private async Task<Nexora.Application.Abstractions.Messaging.Result<T>> SendAsync<T>(Nexora.Application.Abstractions.Messaging.ICommand<T> command)
    {
        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId: null));
        var sender = provider.GetRequiredService<ISender>();
        return await sender.Send(command);
    }

    private async Task<Nexora.Application.Abstractions.Messaging.Result> SendAsync(Nexora.Application.Abstractions.Messaging.ICommand command)
    {
        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId: null));
        var sender = provider.GetRequiredService<ISender>();
        return await sender.Send(command);
    }

    private async Task<Nexora.Application.Abstractions.Messaging.Result<TenantOwnershipResponse>> SendQueryAsync(Guid tenantId)
    {
        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId: null));
        var sender = provider.GetRequiredService<ISender>();
        return await sender.Send(new GetTenantOwnershipQuery(tenantId));
    }
}
