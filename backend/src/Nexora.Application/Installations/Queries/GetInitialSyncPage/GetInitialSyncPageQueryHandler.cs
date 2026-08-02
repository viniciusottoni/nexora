using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Contracts.Installations;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Installations.Queries.GetInitialSyncPage;

/// <summary>
/// Porta de <c>PrismaInitialSyncReader.pull</c> — o caso de uso mais complexo do módulo.
/// <para>
/// <b>Modelo de paginação</b>: o cursor é um índice inteiro sobre a concatenação lógica, na
/// ordem fixa abaixo, de 11 listas (8 de catálogo + 3 de autorização). Cada chamada consome
/// <c>Limit</c> itens dessa sequência a partir de <c>Cursor</c>, podendo cruzar a fronteira
/// entre listas — exatamente o algoritmo de <c>readPage</c> no leitor original. Isso evita ter
/// once cursor por lista (mais simples para o cliente: só guarda um inteiro) ao custo de uma
/// contagem (<c>COUNT</c>) de cada fonte a cada chamada — aceitável no volume de um cardápio de
/// pizzaria (doc. 03, §14).
/// </para>
/// <para>
/// <b>Por que filtra tenant/store explicitamente em vez de confiar só em RLS</b>: este endpoint
/// é autenticado pelo protocolo de assinatura da instalação
/// (<c>AuthenticateInstallationRequestCommand</c>), não por JWT — não há garantia, neste
/// pacote, de que o interceptor de conexão do EF Core (ADR-004) já esteja resolvendo
/// <c>ICurrentTenantContext</c> a partir desse fluxo (isso depende de como o módulo de
/// Autenticação, fora do escopo desta tarefa, conectar os dois). Filtrar explicitamente aqui é
/// defesa em profundidade — se/quando a RLS estiver comprovadamente ativa para este fluxo, o
/// filtro explícito continua correto (redundante, não incorreto).
/// </para>
/// </summary>
internal sealed class GetInitialSyncPageQueryHandler
    : IRequestHandler<GetInitialSyncPageQuery, Result<InitialSyncPageResponse>>
{
    private readonly IApplicationDbContext _db;

    public GetInitialSyncPageQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result<InitialSyncPageResponse>> Handle(
        GetInitialSyncPageQuery request, CancellationToken cancellationToken)
    {
        var tenantId = request.TenantId;
        var storeId = request.StoreId;

        var installation = await _db.EdgeInstallations.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == request.InstallationId && e.TenantId == tenantId, cancellationToken);

        if (installation is null || !installation.IsInstalled)
        {
            return Result<InitialSyncPageResponse>.Failure(
                "Instalação não encontrada.", ApiErrorCodes.InstallationNotFound);
        }

        var config = await _db.TenantConfigs.AsNoTracking()
            .FirstOrDefaultAsync(c => c.TenantId == tenantId, cancellationToken);

        if (config is null)
        {
            return Result<InitialSyncPageResponse>.Failure(
                "Configuração do tenant não encontrada.", ApiErrorCodes.InstallationNotFound);
        }

        var counts = await CountSourcesAsync(tenantId, storeId, cancellationToken);
        var total = counts.Sum();

        if (request.Cursor > total || (request.Cursor == total && request.Cursor > 0))
        {
            return Result<InitialSyncPageResponse>.Success(new InitialSyncPageResponse(
                Array.Empty<InitialSyncEvent>(), request.Cursor, HasMore: false));
        }

        var window = new PageWindow(request.Cursor, request.Limit);

        // Nota: a materialização das entidades (ToListAsync) acontece antes do mapeamento para
        // o DTO — ToString() de enum e leitura de coleção de string[] nem sempre traduzem para
        // SQL de forma previsível dependendo do provider/versão do EF Core, então o mapeamento
        // roda em memória, já sobre a página (no máximo <c>Limit</c> linhas) — sem custo real.
        var stations = await window.ConsumeAsync(counts[0], async (skip, take) =>
            (await _db.Stations.AsNoTracking().Where(x => x.TenantId == tenantId)
                .OrderBy(x => x.SortOrder).ThenBy(x => x.Id).Skip(skip).Take(take)
                .ToListAsync(cancellationToken))
            .Select(x => new InitialSyncStation(
                x.Id, x.TenantId, x.StoreId, x.Code, x.Name, x.Type.ToString(),
                x.CapacitySlots, x.AvgCookSeconds, x.SortOrder, x.IsActive,
                x.CreatedAt, x.UpdatedAt, x.DeletedAt))
            .ToList());

        var categories = await window.ConsumeAsync(counts[1], async (skip, take) =>
            (await _db.Categories.AsNoTracking().Where(x => x.TenantId == tenantId)
                .OrderBy(x => x.SortOrder).ThenBy(x => x.Id).Skip(skip).Take(take)
                .ToListAsync(cancellationToken))
            .Select(x => new InitialSyncCategory(
                x.Id, x.TenantId, x.Name, x.Description, x.SortOrder, x.AvailableSchedule,
                x.IsActive, x.CreatedAt, x.UpdatedAt, x.DeletedAt))
            .ToList());

        var products = await window.ConsumeAsync(counts[2], async (skip, take) =>
            (await _db.Products.AsNoTracking().Where(x => x.TenantId == tenantId)
                .OrderBy(x => x.SortOrder).ThenBy(x => x.Id).Skip(skip).Take(take)
                .ToListAsync(cancellationToken))
            .Select(x => new InitialSyncProduct(
                x.Id, x.TenantId, x.CategoryId, x.StationId, x.Name, x.Description,
                x.IngredientsText, x.Allergens.ToList(), x.SortOrder, x.IsActive, x.IsAvailable,
                x.UnavailableReason, x.UnavailableSince, x.AllowsFractions, x.MaxFractions,
                x.FractionGroup, x.Ncm, x.Cest, x.Cfop, x.OriginCode,
                x.CreatedAt, x.UpdatedAt, x.DeletedAt))
            .ToList());

        var variants = await window.ConsumeAsync(counts[3], async (skip, take) =>
            (await _db.ProductVariants.AsNoTracking().Where(x => x.TenantId == tenantId)
                .OrderBy(x => x.Id).Skip(skip).Take(take)
                .ToListAsync(cancellationToken))
            .Select(x => new InitialSyncProductVariant(
                x.Id, x.TenantId, x.ProductId, x.Name, x.Sku, x.SizeCode, x.PrepMinutes,
                x.IsDefault, x.IsActive, x.FiscalRates, x.CreatedAt, x.UpdatedAt, x.DeletedAt))
            .ToList());

        var prices = await window.ConsumeAsync(counts[4], async (skip, take) =>
            (await _db.Prices.AsNoTracking().Where(x => x.TenantId == tenantId)
                .OrderBy(x => x.ValidFrom).ThenBy(x => x.Id).Skip(skip).Take(take)
                .ToListAsync(cancellationToken))
            .Select(x => new InitialSyncPrice(
                x.Id, x.TenantId, x.VariantId, x.Channel.ToString(), x.Amount,
                x.ValidFrom, x.ValidTo, x.CreatedAt, x.CreatedBy))
            .ToList());

        var modifierGroups = await window.ConsumeAsync(counts[5], async (skip, take) =>
            (await _db.ModifierGroups.AsNoTracking().Where(x => x.TenantId == tenantId)
                .OrderBy(x => x.SortOrder).ThenBy(x => x.Id).Skip(skip).Take(take)
                .ToListAsync(cancellationToken))
            .Select(x => new InitialSyncModifierGroup(
                x.Id, x.TenantId, x.Name, x.MinSelect, x.MaxSelect, x.IsRequired, x.SortOrder,
                x.CreatedAt, x.UpdatedAt, x.DeletedAt))
            .ToList());

        var modifiers = await window.ConsumeAsync(counts[6], async (skip, take) =>
            (await _db.Modifiers.AsNoTracking().Where(x => x.TenantId == tenantId)
                .OrderBy(x => x.GroupId).ThenBy(x => x.SortOrder).ThenBy(x => x.Id).Skip(skip).Take(take)
                .ToListAsync(cancellationToken))
            .Select(x => new InitialSyncModifier(
                x.Id, x.TenantId, x.GroupId, x.Name, x.PriceDelta, x.IngredientId, x.Quantity,
                x.IsAvailable, x.SortOrder, x.CreatedAt, x.UpdatedAt, x.DeletedAt))
            .ToList());

        var productModifierGroups = await window.ConsumeAsync(counts[7], async (skip, take) =>
            (await _db.ProductModifierGroups.AsNoTracking().Where(x => x.TenantId == tenantId)
                .OrderBy(x => x.ProductId).ThenBy(x => x.SortOrder).ThenBy(x => x.GroupId).Skip(skip).Take(take)
                .ToListAsync(cancellationToken))
            .Select(x => new InitialSyncProductModifierGroup(x.TenantId, x.ProductId, x.GroupId, x.SortOrder))
            .ToList());

        var roles = await window.ConsumeAsync(counts[8], async (skip, take) =>
            (await _db.Roles.AsNoTracking().Where(x => x.TenantId == tenantId)
                .OrderBy(x => x.Code).ThenBy(x => x.Id).Skip(skip).Take(take)
                .ToListAsync(cancellationToken))
            .Select(x => new InitialSyncRole(
                x.Id, x.TenantId, x.Code, x.Name, x.Permissions, x.IsSystem,
                x.CreatedAt, x.UpdatedAt, x.DeletedAt))
            .ToList());

        var users = await window.ConsumeAsync(counts[9], async (skip, take) =>
            (await _db.Users.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.PinHash != null && x.PinLookup != null)
                .OrderBy(x => x.Id).Skip(skip).Take(take)
                .ToListAsync(cancellationToken))
            .Select(x => new InitialSyncOperationalUser(
                x.Id, x.TenantId, x.Name, x.PinHash!, x.PinLookup, x.Status.ToString(),
                x.PinRotatedAt, x.CreatedAt, x.UpdatedAt, x.DeletedAt))
            .ToList());

        var userRoles = await window.ConsumeAsync(counts[10], async (skip, take) =>
            (await _db.UserRoles.AsNoTracking()
                .Where(x => x.TenantId == tenantId && (x.StoreId == null || x.StoreId == storeId))
                .Where(x => _db.Users.Any(u => u.Id == x.UserId && u.PinHash != null && u.PinLookup != null))
                .OrderBy(x => x.Id).Skip(skip).Take(take)
                .ToListAsync(cancellationToken))
            .Select(x => new InitialSyncUserRole(x.Id, x.TenantId, x.UserId, x.RoleId, x.StoreId))
            .ToList());

        var nextCursor = total == 0 ? 1 : request.Cursor + window.Loaded;
        var hasMore = request.Cursor + window.Loaded < total;

        var payload = new InitialSyncBootstrapPayload(
            config.ConfigVersion,
            config.CatalogVersion,
            config.Branding,
            config.Operation,
            config.Thresholds,
            config.Modules,
            config.Fiscal,
            config.Printers,
            config.Payments,
            config.Maintenance,
            new InitialSyncCatalogPayload(stations, categories, products, variants, prices, modifierGroups, modifiers, productModifierGroups),
            new InitialSyncAuthorizationPayload(roles, users, userRoles));

        var events = new[] { new InitialSyncEvent(installation.Id, "tenant.config_updated", payload) };

        return Result<InitialSyncPageResponse>.Success(new InitialSyncPageResponse(events, nextCursor, hasMore));
    }

    private async Task<int[]> CountSourcesAsync(Guid tenantId, Guid storeId, CancellationToken cancellationToken)
    {
        return
        [
            await _db.Stations.AsNoTracking().CountAsync(x => x.TenantId == tenantId, cancellationToken),
            await _db.Categories.AsNoTracking().CountAsync(x => x.TenantId == tenantId, cancellationToken),
            await _db.Products.AsNoTracking().CountAsync(x => x.TenantId == tenantId, cancellationToken),
            await _db.ProductVariants.AsNoTracking().CountAsync(x => x.TenantId == tenantId, cancellationToken),
            await _db.Prices.AsNoTracking().CountAsync(x => x.TenantId == tenantId, cancellationToken),
            await _db.ModifierGroups.AsNoTracking().CountAsync(x => x.TenantId == tenantId, cancellationToken),
            await _db.Modifiers.AsNoTracking().CountAsync(x => x.TenantId == tenantId, cancellationToken),
            await _db.ProductModifierGroups.AsNoTracking().CountAsync(x => x.TenantId == tenantId, cancellationToken),
            await _db.Roles.AsNoTracking().CountAsync(x => x.TenantId == tenantId, cancellationToken),
            await _db.Users.AsNoTracking().CountAsync(x => x.TenantId == tenantId && x.PinHash != null && x.PinLookup != null, cancellationToken),
            await _db.UserRoles.AsNoTracking()
                .Where(x => x.TenantId == tenantId && (x.StoreId == null || x.StoreId == storeId))
                .CountAsync(x => _db.Users.Any(u => u.Id == x.UserId && u.PinHash != null && u.PinLookup != null), cancellationToken),
        ];
    }

    /// <summary>
    /// Estado mutável do janelamento sobre a concatenação lógica das 11 fontes — mesma máquina
    /// de estados do <c>readPage</c> original (offset/remaining/loaded), só que tipada por
    /// fonte em vez de percorrer um array de <c>unknown[]</c>.
    /// </summary>
    private sealed class PageWindow(int cursor, int limit)
    {
        private int _offset = cursor;
        private int _remaining = limit;

        public int Loaded { get; private set; }

        public async Task<IReadOnlyList<T>> ConsumeAsync<T>(int sourceCount, Func<int, int, Task<List<T>>> load)
        {
            if (_remaining <= 0)
            {
                return Array.Empty<T>();
            }

            if (_offset >= sourceCount)
            {
                _offset -= sourceCount;
                return Array.Empty<T>();
            }

            var take = Math.Min(_remaining, sourceCount - _offset);
            var items = await load(_offset, take);

            Loaded += items.Count;
            _remaining -= items.Count;
            _offset = 0;

            return items;
        }
    }
}
