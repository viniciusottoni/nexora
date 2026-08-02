using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Tenants;

namespace Nexora.Application.Tenants.Commands.ProvisionTenant;

/// <summary>
/// Provisiona um novo tenant: cria estabelecimento, configuração, loja padrão, papéis e praças
/// do template de negócio, convida o dono por e-mail e emite o token de instalação do edge.
/// Porta de <c>provision-tenants.service.ts</c> (rota <c>POST /v1/platform/tenants</c>).
/// </summary>
public sealed record ProvisionTenantCommand(
    string Name,
    string Slug,
    string Plan,
    string Template,
    string OwnerName,
    string OwnerEmail,
    string StoreName,
    string StoreTimezone) : ICommand<ProvisionTenantResponse>;
