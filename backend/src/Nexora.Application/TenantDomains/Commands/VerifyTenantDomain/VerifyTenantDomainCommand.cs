using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Platform;

namespace Nexora.Application.TenantDomains.Commands.VerifyTenantDomain;

/// <summary>
/// Porta de <c>POST /v1/platform/domains/{id}/verify</c> (US-143 §7). Implementa
/// <see cref="IPersistsStateOnFailureCommand"/> porque uma tentativa de verificação sem o registro
/// DNS ainda precisa persistir <c>TenantDomain.MarkVerificationFailed</c> (bump de
/// <c>UpdatedAt</c>, sinal de "última tentativa") mesmo devolvendo <c>Result.Failure</c> — mesmo
/// idioma de <c>PairDeviceCommand</c> (contador de tentativas).
/// </summary>
public sealed record VerifyTenantDomainCommand(Guid DomainId)
    : ICommand<VerifyTenantDomainResponse>, IPersistsStateOnFailureCommand;
