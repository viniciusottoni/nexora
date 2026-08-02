using Nexora.Application.Installation.Abstractions;
using Microsoft.Extensions.Options;

namespace Nexora.Infrastructure.Installation;

/// <summary>Porta do <c>poll()</c> em <c>sync-worker.ts</c> — GET assinado em <c>{SyncEndpoint}/v1/sync/health</c>.</summary>
public sealed class SyncHealthPoller : ISyncHealthPoller
{
    private readonly HttpClient _httpClient;
    private readonly IInstallationRequestSigner _signer;
    private readonly EdgeInstallationIdentityOptions _options;

    public SyncHealthPoller(HttpClient httpClient, IInstallationRequestSigner signer, IOptions<EdgeInstallationIdentityOptions> options)
    {
        _httpClient = httpClient;
        _signer = signer;
        _options = options.Value;
    }

    public async Task<SyncHealthPollResult> PollAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.SyncEndpoint))
        {
            return new SyncHealthPollResult(DependencyStatus.Unknown, HttpStatusCode: null);
        }

        try
        {
            var target = new Uri($"{_options.SyncEndpoint.TrimEnd('/')}/v1/sync/health");
            using var request = new HttpRequestMessage(HttpMethod.Get, target);

            foreach (var (key, value) in await _signer.SignAsync(HttpMethod.Get.Method, target, cancellationToken))
            {
                request.Headers.TryAddWithoutValidation(key, value);
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            using var response = await _httpClient.SendAsync(request, cts.Token);
            var status = response.IsSuccessStatusCode ? DependencyStatus.Ok : DependencyStatus.Degraded;
            return new SyncHealthPollResult(status, (int)response.StatusCode);
        }
        catch
        {
            // Sem conexão com a nuvem não é erro visível ao operador — é o cenário normal de
            // operação offline (ADR-001/ADR-021) — reporta DOWN, não propaga a exceção.
            return new SyncHealthPollResult(DependencyStatus.Down, HttpStatusCode: null);
        }
    }
}
