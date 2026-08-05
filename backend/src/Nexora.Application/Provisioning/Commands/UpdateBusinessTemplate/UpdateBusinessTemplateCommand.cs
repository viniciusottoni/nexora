using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Platform;

namespace Nexora.Application.Provisioning.Commands.UpdateBusinessTemplate;

/// <summary>
/// Porta de <c>PUT /v1/platform/templates/{code}</c> (US-142 §4, cenário "Atualização de modelo") —
/// edita o modelo pela Replay e incrementa <c>business_template.version</c>
/// (<see cref="Nexora.Domain.Provisioning.BusinessTemplate.Update"/>). Tenants já provisionados
/// guardam a versão ANTERIOR em <c>tenant_config.template_code</c>/<c>.template_version</c>
/// (materializada no momento do provisionamento) e nunca são alterados por esta operação — não há
/// nenhuma referência viva de tenant a <c>business_template</c> para propagar.
/// </summary>
public sealed record UpdateBusinessTemplateCommand(
    string Code, string Name, string ConfigJson, string SeedsJson) : ICommand<BusinessTemplateDetailResponse>;
