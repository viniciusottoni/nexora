using Nexora.Application.Abstractions.Platform;

namespace Nexora.IntegrationTests.Fakes;

/// <summary>Duplo de <see cref="ICertificateIssuer"/> que sempre falha — usado pelo cenário "Falha de renovação" (US-143 §4).</summary>
internal sealed class FakeCertificateIssuer : ICertificateIssuer
{
    public Task<CertificateIssuanceResult> IssueAsync(string domain, CancellationToken cancellationToken) =>
        Task.FromResult(CertificateIssuanceResult.Failed);
}
