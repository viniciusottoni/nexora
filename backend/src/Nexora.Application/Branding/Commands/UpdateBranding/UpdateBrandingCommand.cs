using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Branding;

namespace Nexora.Application.Branding.Commands.UpdateBranding;

/// <summary>Atualiza (patch parcial) a identidade visual do tenant autenticado. Porta de <c>PATCH /v1/tenant/branding</c>.</summary>
public sealed record UpdateBrandingCommand(UpdateBrandingRequest Patch) : ICommand<UpdateBrandingResponse>;
