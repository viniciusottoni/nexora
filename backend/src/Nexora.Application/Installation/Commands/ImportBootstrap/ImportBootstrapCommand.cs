using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Installation;

namespace Nexora.Application.Installation.Commands.ImportBootstrap;

/// <summary>
/// Importa a carga de primeira subida do edge (ADR-019) — porta de <c>persistBootstrap</c>/
/// <c>parseBootstrap</c> (import-bootstrap.ts). Aceita uma ou mais páginas de configuração
/// (o cliente de instalação faz um ou mais GET /v1/sync/pull na nuvem e reúne tudo aqui em
/// uma única chamada) porque o mesmo evento sintético <c>tenant.config_updated</c> pode chegar
/// paginado quando o catálogo é grande — mesma razão de existir do <c>mergePages</c> original.
/// </summary>
public sealed record ImportBootstrapCommand(
    BootstrapTenantIdentity Tenant,
    BootstrapStoreIdentity Store,
    BootstrapInstallationIdentity Installation,
    IReadOnlyList<BootstrapConfigSection> ConfigPages) : ICommand<ImportBootstrapResponse>;
