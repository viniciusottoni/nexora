using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Platform;

namespace Nexora.Application.TenantDomains.Commands.RegisterTenantDomain;

/// <summary>Porta de <c>POST /v1/platform/tenants/{id}/domains</c> (US-143 §7).</summary>
public sealed record RegisterTenantDomainCommand(Guid TenantId, string Domain) : ICommand<RegisterTenantDomainResponse>;
