namespace Nexora.Application.Installation.Abstractions;

/// <summary>
/// Ping TCP simples no Redis local do edge (mesmo protocolo <c>PING</c>/<c>+PONG</c> usado pelo
/// probe original em TypeScript) — implementado em Infrastructure, sem StackExchange.Redis
/// aqui em Application (ADR-039).
/// </summary>
public interface IRedisHealthChecker
{
    Task<DependencyStatus> PingAsync(CancellationToken cancellationToken);
}
