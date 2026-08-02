using Nexora.Application.Abstractions.Security;

namespace Nexora.IntegrationTests.Fakes;

/// <summary>
/// Duplo de teste de <see cref="ICurrentTenantContext"/> com <see cref="TenantId"/> fixo — usado
/// tanto para simular uma requisição autenticada da nuvem (um valor por instância de teste)
/// quanto o comportamento do edge (ADR-004: "uma loja = um tenant", <c>TenantId</c> nunca muda em
/// tempo de execução) — a US-001 pede que os dois se comportem de forma idêntica (cenário
/// Gherkin "Isolamento no servidor local"), e usar o MESMO tipo de duplo para representar ambos
/// prova exatamente isso: nenhuma ramificação de código depende de qual API está rodando.
/// </summary>
/// <remarks>
/// Parâmetros além de <c>tenantId</c> são opcionais (US-005: os handlers de Devices também
/// exigem <see cref="StoreId"/>/<see cref="UserId"/> no contexto, ausentes na US-001 original) —
/// aditivo de propósito, para não quebrar nenhuma chamada posicional já existente
/// (<c>new StaticTenantContext(tenantId)</c> continua válida).
/// </remarks>
public sealed class StaticTenantContext : ICurrentTenantContext
{
    public StaticTenantContext(
        Guid? tenantId,
        Guid? storeId = null,
        Guid? userId = null,
        Guid? deviceId = null,
        IReadOnlyList<string>? permissions = null)
    {
        TenantId = tenantId;
        StoreId = storeId;
        UserId = userId;
        DeviceId = deviceId;
        Permissions = permissions ?? Array.Empty<string>();
    }

    public Guid? TenantId { get; }
    public Guid? StoreId { get; }
    public Guid? UserId { get; }
    public IReadOnlyList<string> Roles { get; } = Array.Empty<string>();
    public IReadOnlyList<string> Permissions { get; }
    public Guid? DeviceId { get; }
    public Guid? SessionId => null;
}
