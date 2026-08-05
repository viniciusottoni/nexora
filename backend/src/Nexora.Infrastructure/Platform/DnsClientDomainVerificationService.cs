using Nexora.Application.Abstractions.Platform;
using DnsClient;

namespace Nexora.Infrastructure.Platform;

/// <summary>
/// Implementação real de <see cref="IDomainVerificationService"/> (US-143 §3.1) — consulta DNS
/// TXT público de verdade via <c>DnsClient.NET</c> (MIT, pacote <c>DnsClient</c>). O BCL do .NET
/// (<c>System.Net.Dns</c>) não expõe consulta de registro TXT nativamente (só resolução de
/// A/AAAA/PTR via <c>GetHostEntry</c>), daí a dependência externa — a mesma lacuna documentada em
/// vários outros pontos deste repositório (ex.: Ed25519 via BouncyCastle). Leitura de saída pura,
/// sem efeito colateral: seguro rodar de verdade em qualquer ambiente (dev/CI/produção), ao
/// contrário de <see cref="ManualCertificateIssuer"/>.
/// </summary>
public sealed class DnsClientDomainVerificationService : IDomainVerificationService
{
    private readonly ILookupClient _lookupClient;

    public DnsClientDomainVerificationService()
    {
        _lookupClient = new LookupClient(new LookupClientOptions
        {
            UseCache = false,
            Timeout = TimeSpan.FromSeconds(5),
            Retries = 1,
            ThrowDnsErrors = false,
        });
    }

    public async Task<bool> HasTxtRecordAsync(string recordName, string expectedValue, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(recordName) || string.IsNullOrWhiteSpace(expectedValue))
        {
            return false;
        }

        try
        {
            var response = await _lookupClient.QueryAsync(recordName, QueryType.TXT, cancellationToken: cancellationToken);
            if (response.HasError)
            {
                return false;
            }

            return response.Answers.TxtRecords()
                .SelectMany(record => record.Text)
                .Any(text => string.Equals(text?.Trim(), expectedValue, StringComparison.Ordinal));
        }
        catch (DnsResponseException)
        {
            // Domínio inexistente/sem registro/servidor DNS recusou — tratado como "não
            // verificado" (US-143 §4, cenário "Domínio não verificado"), nunca uma exceção não
            // tratada estourando 500.
            return false;
        }
    }
}
