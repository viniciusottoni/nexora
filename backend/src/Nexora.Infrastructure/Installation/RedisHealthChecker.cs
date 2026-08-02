using System.Net.Sockets;
using System.Text;
using Nexora.Application.Installation.Abstractions;
using Microsoft.Extensions.Options;

namespace Nexora.Infrastructure.Installation;

public sealed class RedisHealthCheckOptions
{
    public const string SectionName = "Redis";

    public string Host { get; set; } = "redis";
    public int Port { get; set; } = 6379;
}

/// <summary>
/// Ping TCP bruto no protocolo RESP (<c>*1\r\n$4\r\nPING\r\n</c> → <c>+PONG</c>), igual ao probe
/// original — evita abrir uma conexão gerenciada do StackExchange.Redis só para um health check
/// pontual (o cliente compartilhado, quando existir, é gerenciado por outro módulo).
/// </summary>
public sealed class RedisHealthChecker : IRedisHealthChecker
{
    private static readonly byte[] PingCommand = Encoding.ASCII.GetBytes("*1\r\n$4\r\nPING\r\n");

    private readonly RedisHealthCheckOptions _options;

    public RedisHealthChecker(IOptions<RedisHealthCheckOptions> options)
    {
        _options = options.Value;
    }

    public async Task<DependencyStatus> PingAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var client = new TcpClient();
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(2));

            await client.ConnectAsync(_options.Host, _options.Port, timeoutCts.Token);

            var stream = client.GetStream();
            await stream.WriteAsync(PingCommand, timeoutCts.Token);

            var buffer = new byte[64];
            var read = await stream.ReadAsync(buffer, timeoutCts.Token);
            var response = Encoding.ASCII.GetString(buffer, 0, read);

            return response.StartsWith("+PONG", StringComparison.Ordinal) ? DependencyStatus.Ok : DependencyStatus.Degraded;
        }
        catch
        {
            return DependencyStatus.Down;
        }
    }
}
