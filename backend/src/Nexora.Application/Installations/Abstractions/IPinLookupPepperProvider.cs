namespace Nexora.Application.Installations.Abstractions;

/// <summary>
/// Deriva o "pepper" de lookup de PIN exclusivo de um tenant (HMAC-SHA256 de uma chave mestra
/// com o tenantId — mesma derivação de <c>deriveTenantPinPepper</c> em
/// <c>prisma-installation-registration.repository.ts</c>). Entregue ao edge **apenas** na
/// resposta de registro assinada por TLS — nunca sincronizado depois (ADR-031).
/// </summary>
public interface IPinLookupPepperProvider
{
    string Derive(Guid tenantId);
}
