namespace Nexora.Shared.Errors;

/// <summary>
/// US-033 (Cancelar item ou pedido com autorização) — códigos próprios desta história, em arquivo
/// separado (convenção da classe <c>partial</c>, ver docstring de <see cref="ApiErrorCodes"/>) para
/// não colidir com edições concorrentes de outras histórias em paralelo (US-031/US-035) que também
/// tocam <c>ApiErrorCodes.Operation.cs</c>.
/// </summary>
public static partial class ApiErrorCodes
{
    /// <summary>
    /// Transição de estado recusada por regra de negócio explícita do documento 04 — US-033 §4,
    /// cenário "Pedido fechado não cancela" (a orientação aponta o fluxo de estorno, RF-CXA-13/
    /// Fase 2) e as guardas equivalentes de item já servido/já cancelado.
    /// </summary>
    public const string InvalidStateTransition = "INVALID_STATE_TRANSITION";
}
