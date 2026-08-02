using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Catalog;

namespace Nexora.Application.Catalog.Products.Queries.GetPublicMenu;

/// <summary>
/// Porta de <c>GET /v1/public/menu</c> (US-010 §7) — pública, sem autenticação. O tenant é
/// resolvido pelo domínio customizado (<see cref="Host"/>), mesmo mecanismo de
/// <c>GetPublicBrandingQuery</c> (US-003): não existe hoje nenhum outro jeito de identificar o
/// tenant num endpoint sem JWT (ver docstring de <c>GetPublicMenuQueryHandler</c> — desvio
/// deliberado em relação à assinatura literal do documento da US, que só lista
/// <c>?channel=</c>). <see cref="Channel"/> é aceito para compatibilidade futura com cardápio por
/// canal, mas ainda não filtra nada: nem <c>category</c> nem <c>product</c> têm hoje uma coluna de
/// canal (só <c>Price</c> a terá, em US-011/US-014, fora do escopo desta história) — ver relatório
/// da tarefa.
/// </summary>
public sealed record GetPublicMenuQuery(string Host, string? Channel) : IQuery<PublicMenuResponse>;
