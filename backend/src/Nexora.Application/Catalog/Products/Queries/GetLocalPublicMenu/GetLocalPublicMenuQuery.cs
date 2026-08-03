using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Catalog;

namespace Nexora.Application.Catalog.Products.Queries.GetLocalPublicMenu;

/// <summary>
/// Cardápio público servido pelo edge (US-021 §7: <c>GET /v1/public/menu?channel=DINE_IN</c>) —
/// gêmeo de <c>GetPublicMenuQuery</c> (nuvem), mas sem <c>host</c>: o edge já sabe qual é o seu
/// único tenant (ADR-004, ver <see cref="GetLocalPublicMenuQueryHandler"/>).
/// </summary>
public sealed record GetLocalPublicMenuQuery(string? Channel) : IQuery<PublicMenuResponse>;
