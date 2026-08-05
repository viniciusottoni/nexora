namespace Nexora.Application.Abstractions.Platform;

/// <summary>
/// Verificação de propriedade de domínio por registro DNS TXT (US-143 §3.1/§10) — porta usada por
/// <c>VerifyTenantDomainCommandHandler</c>. Consulta DNS pública real (leitura de saída, sem efeito
/// colateral algum, por isso pode ser implementada de verdade em <c>Nexora.Infrastructure</c> desde
/// já — ao contrário de <see cref="ICertificateIssuer"/>, que precisaria de posse real de domínio
/// e infraestrutura ACME internet-facing para ser genuína (ver docstring de
/// <c>ManualCertificateIssuer</c>).
/// </summary>
public interface IDomainVerificationService
{
    /// <summary>
    /// Verdadeiro quando o registro TXT <paramref name="recordName"/> (ex.:
    /// <c>_verify.cardapio.donabetinha.com.br</c>) existe e contém, entre seus valores,
    /// <paramref name="expectedValue"/> (o <c>VerificationToken</c> gerado no cadastro).
    /// </summary>
    Task<bool> HasTxtRecordAsync(string recordName, string expectedValue, CancellationToken cancellationToken);
}
