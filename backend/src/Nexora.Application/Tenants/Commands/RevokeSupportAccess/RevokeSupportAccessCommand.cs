using Nexora.Application.Abstractions.Messaging;

namespace Nexora.Application.Tenants.Commands.RevokeSupportAccess;

/// <summary>
/// US-145, cenário Gherkin "Revogação pelo cliente" — o gestor do tenant encerra uma concessão de
/// acesso de suporte ativa. Cessa imediatamente: nenhuma verificação posterior de
/// <see cref="Domain.Platform.SupportAccess.IsActive"/> voltará a ser <c>true</c> depois deste
/// comando (idempotente — revogar duas vezes não é erro, ver <see cref="Domain.Platform.SupportAccess.Revoke"/>).
/// </summary>
/// <param name="TenantId">Tenant do CHAMADOR (nunca do corpo da requisição — resolvido por <c>ICurrentTenantContext</c>).</param>
/// <param name="SupportAccessId">Concessão a revogar.</param>
/// <param name="RevokedBy">Usuário do tenant que executou a revogação.</param>
public sealed record RevokeSupportAccessCommand(
    Guid TenantId,
    Guid SupportAccessId,
    Guid? RevokedBy) : ICommand;
