using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Catalog.Products.Queries.GetPublicMenu;
using Nexora.Contracts.Catalog;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Catalog.Products.Queries.GetLocalPublicMenu;

/// <summary>
/// US-021 §7 <c>GET /v1/public/menu?channel=DINE_IN</c> servido pelo edge. Reaproveita
/// <see cref="PublicMenuBuilder"/> (o mesmo núcleo de <c>GetPublicMenuQueryHandler</c>, nuvem) —
/// única diferença é como o tenant é descoberto: aqui vem de <see cref="ICurrentTenantContext.TenantId"/>
/// (sempre fixo por instalação, ADR-004 "uma loja = um tenant" no edge), nunca de um parâmetro
/// <c>host</c> como na nuvem (o edge roda na LAN da loja, sem domínio público).
/// </summary>
/// <remarks>
/// [DECISÃO DOCUMENTADA] <c>Categories</c>/<c>Products</c>/<c>Prices</c> são lidos do MESMO
/// <c>AppDbContext</c>/schema do edge (não existe uma cópia paralela) — mas o worker de sync
/// completo (E-06) que replicaria o cardápio editado na nuvem para o Postgres local ainda não
/// existe nesta solution (confirmado: só <c>SyncOutboxWorker</c>/<c>PollSyncHealthCommand</c>
/// rodam hoje, e cobrem saúde de sincronização, não conteúdo). Pragmaticamente, esta consulta lê o
/// que já estiver no Postgres do edge — hoje, isso significa cardápio vazio numa instalação nova
/// até o catálogo ser semeado manualmente ou até o worker de sync de conteúdo ser implementado
/// (fora do escopo de US-021/US-022). Não é um bug desta história: é a mesma limitação que
/// qualquer consulta de catálogo no edge teria hoje, documentada aqui para não ser redescoberta.
/// </remarks>
internal sealed class GetLocalPublicMenuQueryHandler : IRequestHandler<GetLocalPublicMenuQuery, Result<PublicMenuResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public GetLocalPublicMenuQueryHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<PublicMenuResponse>> Handle(GetLocalPublicMenuQuery request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is not { } tenantId)
        {
            return Result<PublicMenuResponse>.Failure(
                "Não foi possível identificar o estabelecimento desta instalação.", ApiErrorCodes.TenantContextMissing);
        }

        var tenantName = await _db.Tenants
            .AsNoTracking()
            .Where(t => t.Id == tenantId && t.DeletedAt == null)
            .Select(t => t.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        var response = await PublicMenuBuilder.BuildAsync(_db, tenantId, tenantName, request.Channel, cancellationToken);

        return Result<PublicMenuResponse>.Success(response);
    }
}
