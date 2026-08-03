using Nexora.Contracts.Tables;

namespace Nexora.Application.Abstractions.Realtime;

/// <summary>
/// Porta de saída para o WebSocket local do mapa de mesas (US-023 §7/§10, ADR-011) — a
/// implementação real (<c>SignalRTableMapBroadcaster</c>, em <c>Nexora.Api.Edge</c>, único lugar
/// que pode referenciar SignalR/ASP.NET Core, ADR-039) publica no grupo <c>tenant:{id}</c> do
/// <c>TableMapHub</c>. Vive em Application (não em Infrastructure) pelo mesmo motivo de
/// <c>IEmailSender</c>/<c>IBrandingStorage</c>: um handler de Application (futuro, das US-021/022/
/// 024/025/026 que mudam estado de mesa/sessão/item) precisa poder chamar isto sem depender de
/// ASP.NET Core.
/// </summary>
/// <remarks>
/// US-025/US-026 (<c>WaiterCallCoordinator</c>/<c>BillRequestCoordinator</c>) já chamam
/// <see cref="NotifySignalAsync"/> para atualizar o INDICADOR do mapa (<c>flags.waiterCalled</c>/
/// <c>flags.billRequested</c>) sem reconstruir a mesa inteira — o snapshot completo continua vindo
/// de um novo <c>GET /v1/tables</c>, este método só evita que quem já está com o mapa aberto precise
/// esperar o próximo polling para ver o sinal.
/// </remarks>
public interface ITableMapBroadcaster
{
    /// <summary>Publica o estado atualizado de UMA mesa para todos os clientes do tenant (ADR-011, sala <c>tenant:{id}</c>).</summary>
    Task NotifyTableChangedAsync(Guid tenantId, TableMapEntryResponse table, CancellationToken cancellationToken);

    /// <summary>
    /// Publica um evento pontual de sinalização (ex.: <c>table.waiter_called</c>,
    /// <c>order.item.ready</c> — formato do payload no contrato §7 da US-023) sem exigir o estado
    /// completo da mesa — mais barato quando só o indicador mudou, não o valor/tempo.
    /// </summary>
    Task NotifySignalAsync(Guid tenantId, string type, object data, CancellationToken cancellationToken);
}
