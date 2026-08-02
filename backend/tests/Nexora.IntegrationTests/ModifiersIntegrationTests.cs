using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Catalog.ModifierGroups.Commands.CreateModifierGroup;
using Nexora.Application.Catalog.ModifierGroups.Commands.DeleteModifierGroup;
using Nexora.Application.Catalog.ModifierGroups.Commands.UpdateModifierGroup;
using Nexora.Application.Catalog.ModifierGroups.Queries.ListModifierGroups;
using Nexora.Application.Catalog.Modifiers.Commands.CreateModifier;
using Nexora.Application.Catalog.Modifiers.Commands.MarkModifierUnavailable;
using Nexora.Application.Catalog.Modifiers.Commands.UpdateModifier;
using Nexora.Application.Catalog.ProductModifierGroups.Commands.LinkModifierGroupToProduct;
using Nexora.Application.Catalog.ProductModifierGroups.Commands.UnlinkModifierGroupFromProduct;
using Nexora.Domain.Catalog;
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
/// Cenários Gherkin da US-012 (Grupos de modificadores) contra um PostgreSQL real (Testcontainers,
/// mesma <see cref="PostgresFixture"/> da US-001/US-005) e o mesmo pipeline MediatR de produção
/// (Validation -&gt; Logging -&gt; Transaction) — "Reuso de grupo entre produtos", "Preço do
/// adicional somado"/"Remoção sem custo" (via <c>Modifier.PriceDelta</c> persistido) e a exigência
/// de isolamento por permissão (<c>catalog:read</c>/<c>catalog:write</c>). Não cobre a validação de
/// carrinho (mínimo/máximo/obrigatório no momento de montar o pedido) — isso é função pura no
/// frontend (<c>apps/web-admin/src/modifiers/modifier-group-management-page.tsx</c>), fora do
/// escopo de um teste de integração com banco.
/// </summary>
[Collection("Postgres")]
public sealed class ModifiersIntegrationTests
{
    private static readonly string[] CatalogWritePermissions = { "catalog:*" };

    private readonly PostgresFixture _fixture;

    public ModifiersIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Cenário Gherkin "Reuso de grupo entre produtos": grupo vinculado a N produtos (aqui 2, para
    /// manter o teste rápido) — ao atualizar a regra de seleção do grupo, a alteração vale para
    /// todos (mesma FK, sem cópia) e um evento <c>product.updated</c> (EVT-050) é emitido por
    /// produto vinculado, para quem cacheia o cardápio localmente saber que precisa buscar de novo.
    /// </summary>
    [Fact]
    public async Task Atualizar_Grupo_Reusado_Em_Dois_Produtos_Reflete_Nos_Dois_E_Emite_Evento_Por_Produto()
    {
        var tenantId = await SeedTenantAsync();
        var (categoryId, productAId, productBId) = await SeedCategoryAndTwoProductsAsync(tenantId);

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId, permissions: CatalogWritePermissions));
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId, permissions: CatalogWritePermissions));
        var sender = provider.GetRequiredService<ISender>();

        var createResult = await sender.Send(new CreateModifierGroupCommand("Ponto da massa", 1, 1, true, 0));
        createResult.IsSuccess.Should().BeTrue();
        var groupId = createResult.Value!.Id;

        (await sender.Send(new LinkModifierGroupToProductCommand(productAId, groupId, 0))).IsSuccess.Should().BeTrue();
        (await sender.Send(new LinkModifierGroupToProductCommand(productBId, groupId, 0))).IsSuccess.Should().BeTrue();
        var eventIdsBeforeUpdate = await db.DomainEvents
            .Where(e => e.TenantId == tenantId && e.Type == "product.updated")
            .Select(e => e.Id)
            .ToListAsync();

        var updateResult = await sender.Send(new UpdateModifierGroupCommand(groupId, 1, 2));

        updateResult.IsSuccess.Should().BeTrue();
        updateResult.Value!.MinSelect.Should().Be(1);
        updateResult.Value!.MaxSelect.Should().Be(2);
        updateResult.Value!.ProductIds.Should().BeEquivalentTo(new[] { productAId, productBId });

        var group = await db.ModifierGroups.SingleAsync(g => g.Id == groupId);
        group.MinSelect.Should().Be(1, "a mudança de regra vale para os dois produtos, não é copiada por produto");
        group.MaxSelect.Should().Be(2);

        var events = await db.DomainEvents
            .Where(e =>
                e.TenantId == tenantId
                && e.Type == "product.updated"
                && e.AggregateType == "product"
                && !eventIdsBeforeUpdate.Contains(e.Id))
            .ToListAsync();

        events.Select(e => e.AggregateId).Should().BeEquivalentTo(new[] { productAId, productBId },
            "cada produto vinculado ao grupo deve receber seu próprio evento de invalidação de cache");
    }

    [Fact]
    public async Task Atualizar_Grupo_Obrigatorio_Com_Minimo_Zero_E_Recusado()
    {
        var tenantId = await SeedTenantAsync();
        var tenantContext = new StaticTenantContext(tenantId, permissions: CatalogWritePermissions);
        await using var db = _fixture.CreateAppDbContext(tenantContext);
        await using var provider = MediatRTestContainerFactory.Build(db, tenantContext);
        var sender = provider.GetRequiredService<ISender>();
        var createResult = await sender.Send(new CreateModifierGroupCommand("Tamanho", 1, 1, true, 0));

        var result = await sender.Send(new UpdateModifierGroupCommand(createResult.Value!.Id, 0, 1));

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.ValidationError);
    }

    /// <summary>
    /// Cenário Gherkin "Preço do adicional somado" / "Remoção sem custo": o <c>price_delta</c>
    /// persistido é exatamente o que foi cadastrado — positivo soma, zero não altera o preço do
    /// item (o cálculo do total do item em si é do módulo de pedidos, fora desta US).
    /// </summary>
    [Fact]
    public async Task Criar_Modificador_Com_Adicional_E_Remocao_Persiste_PriceDelta_Correto()
    {
        var tenantId = await SeedTenantAsync();

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId, permissions: CatalogWritePermissions));
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId, permissions: CatalogWritePermissions));
        var sender = provider.GetRequiredService<ISender>();

        var groupResult = await sender.Send(new CreateModifierGroupCommand("Adicionais", 0, 3, false, 0));
        var groupId = groupResult.Value!.Id;

        var bordaResult = await sender.Send(new CreateModifierCommand(groupId, "Borda Catupiry", 8.00m, null, null, 0));
        var semCebolaResult = await sender.Send(new CreateModifierCommand(groupId, "Sem cebola", 0m, null, null, 1));

        bordaResult.IsSuccess.Should().BeTrue();
        bordaResult.Value!.PriceDelta.Should().Be(8.00m);

        semCebolaResult.IsSuccess.Should().BeTrue();
        semCebolaResult.Value!.PriceDelta.Should().Be(0m);

        var updatePriceResult = await sender.Send(new UpdateModifierCommand(groupId, bordaResult.Value!.Id, 9.50m));
        updatePriceResult.IsSuccess.Should().BeTrue();
        updatePriceResult.Value!.PriceDelta.Should().Be(9.50m);
    }

    /// <summary>
    /// Segurança (ADR-021/ADR-023): sem a permissão <c>catalog:write</c> (nem <c>catalog:*</c>/<c>*</c>),
    /// qualquer escrita no módulo é recusada com <c>AUTH_PERMISSION_DENIED</c> — checado no próprio
    /// handler de Application porque este módulo não pôde registrar uma AuthorizationPolicy nomeada
    /// em Program.cs (worktree isolado, ver relatório da tarefa).
    /// </summary>
    [Fact]
    public async Task Criar_Grupo_Sem_Permissao_Catalog_Write_E_Recusado()
    {
        var tenantId = await SeedTenantAsync();

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId, permissions: Array.Empty<string>()));
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId, permissions: Array.Empty<string>()));
        var sender = provider.GetRequiredService<ISender>();

        var result = await sender.Send(new CreateModifierGroupCommand("Adicionais", 0, 3, false, 0));

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.AuthPermissionDenied);

        (await db.ModifierGroups.AnyAsync(g => g.TenantId == tenantId)).Should().BeFalse();
    }

    /// <summary>Vincular o mesmo grupo duas vezes ao mesmo produto é recusado (não gera vínculo duplicado).</summary>
    [Fact]
    public async Task Vincular_Grupo_Ja_Vinculado_Ao_Mesmo_Produto_E_Recusado()
    {
        var tenantId = await SeedTenantAsync();
        var (categoryId, productId, _) = await SeedCategoryAndTwoProductsAsync(tenantId);

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId, permissions: CatalogWritePermissions));
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId, permissions: CatalogWritePermissions));
        var sender = provider.GetRequiredService<ISender>();

        var groupId = (await sender.Send(new CreateModifierGroupCommand("Tamanho", 1, 1, true, 0))).Value!.Id;

        (await sender.Send(new LinkModifierGroupToProductCommand(productId, groupId, 0))).IsSuccess.Should().BeTrue();

        var duplicate = await sender.Send(new LinkModifierGroupToProductCommand(productId, groupId, 0));

        duplicate.IsSuccess.Should().BeFalse();
        duplicate.Code.Should().Be(ApiErrorCodes.ProductModifierGroupAlreadyLinked);

        (await db.ProductModifierGroups.CountAsync(pg => pg.ProductId == productId && pg.GroupId == groupId)).Should().Be(1);
    }

    /// <summary>Desvincular um grupo que não está vinculado ao produto é recusado com o código certo, não um 500 genérico.</summary>
    [Fact]
    public async Task Desvincular_Grupo_Nao_Vinculado_E_Recusado()
    {
        var tenantId = await SeedTenantAsync();
        var (categoryId, productId, _) = await SeedCategoryAndTwoProductsAsync(tenantId);

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId, permissions: CatalogWritePermissions));
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId, permissions: CatalogWritePermissions));
        var sender = provider.GetRequiredService<ISender>();

        var groupId = (await sender.Send(new CreateModifierGroupCommand("Tamanho", 1, 1, true, 0))).Value!.Id;

        var result = await sender.Send(new UnlinkModifierGroupFromProductCommand(productId, groupId));

        result.IsSuccess.Should().BeFalse();
        result.Code.Should().Be(ApiErrorCodes.ProductModifierGroupNotLinked);
    }

    /// <summary>
    /// Remover um grupo cascateia soft delete para seus modificadores e preserva o vínculo como
    /// referência histórica — a listagem deixa de trazer o grupo removido (ADR: sem DELETE físico).
    /// </summary>
    [Fact]
    public async Task Remover_Grupo_Cascateia_Para_Modificadores_E_Preserva_Vinculos_Historicos()
    {
        var tenantId = await SeedTenantAsync();
        var (categoryId, productId, _) = await SeedCategoryAndTwoProductsAsync(tenantId);

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId, permissions: CatalogWritePermissions));
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId, permissions: CatalogWritePermissions));
        var sender = provider.GetRequiredService<ISender>();

        var groupId = (await sender.Send(new CreateModifierGroupCommand("Adicionais", 0, 3, false, 0))).Value!.Id;
        var modifierId = (await sender.Send(new CreateModifierCommand(groupId, "Bacon", 5m, null, null, 0))).Value!.Id;
        await sender.Send(new LinkModifierGroupToProductCommand(productId, groupId, 0));

        var deleteResult = await sender.Send(new DeleteModifierGroupCommand(groupId));
        deleteResult.IsSuccess.Should().BeTrue();

        var group = await db.ModifierGroups.SingleAsync(g => g.Id == groupId);
        group.DeletedAt.Should().NotBeNull();

        var modifier = await db.Modifiers.SingleAsync(m => m.Id == modifierId);
        modifier.DeletedAt.Should().NotBeNull();

        (await db.ProductModifierGroups.AnyAsync(pg => pg.GroupId == groupId)).Should().BeTrue(
            "a role app_user não possui DELETE e o grupo soft-deletado já torna o vínculo inativo");

        var listResult = await sender.Send(new ListModifierGroupsQuery());
        listResult.Value!.Items.Should().NotContain(g => g.Id == groupId);
    }

    /// <summary>Marcar indisponível não remove o cadastro — só some do que o cliente pode escolher (o KDS ainda vê o histórico).</summary>
    [Fact]
    public async Task Marcar_Modificador_Indisponivel_Nao_Remove_O_Cadastro()
    {
        var tenantId = await SeedTenantAsync();

        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId, permissions: CatalogWritePermissions));
        await using var provider = MediatRTestContainerFactory.Build(db, new StaticTenantContext(tenantId, permissions: CatalogWritePermissions));
        var sender = provider.GetRequiredService<ISender>();

        var groupId = (await sender.Send(new CreateModifierGroupCommand("Adicionais", 0, 3, false, 0))).Value!.Id;
        var modifierId = (await sender.Send(new CreateModifierCommand(groupId, "Bacon", 5m, null, null, 0))).Value!.Id;

        var result = await sender.Send(new MarkModifierUnavailableCommand(groupId, modifierId));

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsAvailable.Should().BeFalse();

        var modifier = await db.Modifiers.SingleAsync(m => m.Id == modifierId);
        modifier.DeletedAt.Should().BeNull("indisponível é status, não exclusão — o histórico e o cadastro continuam existindo");
    }

    private async Task<Guid> SeedTenantAsync()
    {
        var tenantId = Guid.NewGuid();

        await using var db = _fixture.CreateAppDbContext(tenantContext: null);
        db.Tenants.Add(Tenant.Create(tenantId, $"tenant-{tenantId:N}", "Tenant de teste"));
        await db.SaveChangesAsync();

        return tenantId;
    }

    private async Task<(Guid CategoryId, Guid ProductAId, Guid ProductBId)> SeedCategoryAndTwoProductsAsync(Guid tenantId)
    {
        await using var db = _fixture.CreateAppDbContext(new StaticTenantContext(tenantId));

        var category = Category.Create(tenantId, "Pizzas");
        db.Categories.Add(category);

        var productA = Product.Create(tenantId, category.Id, "Pizza Calabresa");
        var productB = Product.Create(tenantId, category.Id, "Pizza Marguerita");
        db.Products.Add(productA);
        db.Products.Add(productB);

        await db.SaveChangesAsync();

        return (category.Id, productA.Id, productB.Id);
    }
}
