using Nexora.Application.Abstractions.Messaging;

namespace Nexora.Application.TenantDomains.Commands.RenewTenantDomainCertificate;

/// <summary>
/// Renova (ou tenta) o certificado de UM domínio de UM tenant — despachado pelo
/// <c>TenantDomainCertificateRenewalWorker</c> por domínio, nunca em lote dentro de um único
/// comando (mesmo padrão de <c>InstallationHealthEvaluationWorker</c>/
/// <c>EvaluateInstallationHealthCommand</c>: o worker varre tenants/domínios fora da transação, o
/// comando cobre exatamente um <c>app.tenant_id</c> por vez — ADR-006, <c>SaveChangesAsync</c>
/// único por comando).
/// </summary>
public sealed record RenewTenantDomainCertificateCommand(Guid TenantId, Guid DomainId)
    : ICommand<bool>, IPersistsStateOnFailureCommand;
