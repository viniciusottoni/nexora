using Nexora.Application.Abstractions.Platform;

namespace Nexora.IntegrationTests.Fakes;

/// <summary>
/// Duplo de <see cref="IDomainVerificationService"/> para os testes de integração da US-143 —
/// evita consulta DNS real de verdade em CI (o adapter real, <c>DnsClientDomainVerificationService</c>,
/// já é exercitado indiretamente pelo próprio contrato da porta; aqui só o resultado importa).
/// </summary>
internal sealed class FakeDomainVerificationService : IDomainVerificationService
{
    private readonly bool _result;

    public FakeDomainVerificationService(bool result)
    {
        _result = result;
    }

    public Task<bool> HasTxtRecordAsync(string recordName, string expectedValue, CancellationToken cancellationToken) =>
        Task.FromResult(_result);
}
