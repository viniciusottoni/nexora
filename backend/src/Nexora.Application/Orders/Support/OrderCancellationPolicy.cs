using Nexora.Domain.Operation;

namespace Nexora.Application.Orders.Support;

/// <summary>
/// US-033 (Cancelar item ou pedido com autorização) §4/§5 (RN-011) — deriva se um cancelamento
/// exige elevação pontual (ADR-023) a partir do ESTADO do item IMEDIATAMENTE ANTES do
/// cancelamento (<c>wasStarted</c> no contrato de API, US-033 §7). Único critério normativo:
/// o item já saiu da fila (<c>Status != Queued</c>, equivalente a <c>FiredAt != null</c>) — a
/// máquina de estados do documento 04 lista <c>IN_PRODUCTION → CANCELLED</c> (qualquer estado a
/// partir de <c>FIRED</c>) como transição que exige autorização de perfil superior; só
/// <c>QUEUED</c> cancela livre (cenário "Cancelamento antes do início da produção").
/// </summary>
public static class OrderCancellationPolicy
{
    public static bool RequiresAuthorization(OrderItemStatus statusBeforeCancel) =>
        statusBeforeCancel is not OrderItemStatus.Queued;
}
