namespace Nexora.Domain.Metrics;

/// <summary>
/// Catálogo dos tipos de <see cref="Alert"/> emitidos pela mesa/comanda (US-025 "Chamar garçom pela
/// mesa" e US-026 "Solicitar a conta") — <see cref="Alert.Type"/> é texto livre (ver
/// <c>AlertConfiguration</c>, sem enum nativo), então esta classe existe só para as duas camadas
/// (Application, que cria os alertas, e Api.Edge, no worker de escalonamento) compartilharem a
/// MESMA string sem replicense o literal em cada lugar — evita o tipo clássico de bug de "digitei
/// WAITER_CALLED errado num dos dois lugares e o filtro nunca casa".
/// </summary>
public static class AlertTypes
{
    /// <summary>EVT-021 <c>table.waiter_called</c> — cliente chamou o garçom pela mesa (US-025).</summary>
    public const string WaiterCalled = "WAITER_CALLED";

    /// <summary>EVT-022 <c>table.bill_requested</c> — conta solicitada, sessão em <c>BILL_REQUESTED</c> (US-026).</summary>
    public const string BillRequested = "BILL_REQUESTED";
}
