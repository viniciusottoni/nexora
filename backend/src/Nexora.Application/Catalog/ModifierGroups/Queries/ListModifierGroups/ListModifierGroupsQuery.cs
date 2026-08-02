using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Catalog;

namespace Nexora.Application.Catalog.ModifierGroups.Queries.ListModifierGroups;

/// <summary>Lista os grupos de modificadores do tenant autenticado, com modificadores e produtos vinculados. Porta de <c>GET /v1/catalog/modifier-groups</c> (US-012).</summary>
public sealed record ListModifierGroupsQuery : IQuery<ModifierGroupListResponse>;
