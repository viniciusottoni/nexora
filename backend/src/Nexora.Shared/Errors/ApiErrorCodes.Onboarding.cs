namespace Nexora.Shared.Errors;

/// <summary>
/// Códigos de erro do roteiro de implantação autoatendido (US-141, ADR-021).
/// </summary>
/// <remarks>
/// [PENDÊNCIA DOCUMENTADA] Estes dois códigos ainda NÃO têm entrada em
/// <c>Nexora.Api.Cloud.Infrastructure.ResultExtensions.MapErrorCode</c> — essa tarefa foi
/// deliberadamente instruída a não editar aquele arquivo (edição paralela por outro agente). Sem a
/// entrada, os dois caem hoje no catch-all (<c>500 INTERNAL_ERROR</c>) em vez do status pretendido
/// abaixo. Ver relatório final da tarefa que introduziu este arquivo para o texto exato do switch a
/// adicionar.
/// </remarks>
public static partial class ApiErrorCodes
{
    /// <summary>
    /// <c>POST /v1/platform/tenants/{id}/activate</c> com pelo menos um dos oito passos anteriores
    /// (todos exceto <c>ACTIVATION</c>) ainda não <c>DONE</c> (US-141 §7/§4, cenário "Validação
    /// antes da ativação"). Pretende mapear para <c>422 Unprocessable Entity</c>, <c>recoverable=true</c>,
    /// <c>requiresAuthorization=false</c> — mesma família de <see cref="PendingItems"/> (bloqueio
    /// contornável ao completar o que falta, não uma falha de autorização). <c>meta.pendingItems</c>
    /// (ver <c>Nexora.Application.Tables.Support.PendingItemsClosePolicy.MetaErrorsKey</c>, mecanismo
    /// reaproveitado) traz a lista de chaves de passo pendentes — o contrato da US-141 usa o nome
    /// <c>meta.pending</c>; ver a mesma nota de pendência acima sobre por que o nome de campo saiu
    /// como <c>pendingItems</c> em vez de <c>pending</c>.
    /// </summary>
    public const string OnboardingIncomplete = "ONBOARDING_INCOMPLETE";

    /// <summary>
    /// <c>PATCH /v1/platform/tenants/{id}/onboarding/{key}</c> com uma chave de passo que não existe
    /// no roteiro (nove chaves fixas, <c>OnboardingStepKeyWireFormat</c>) — nunca deveria acontecer
    /// com um cliente que segue o contrato, mas evita expor exceção não tratada para entrada
    /// inválida. Pretende mapear para <c>404 Not Found</c>, <c>recoverable=false</c>.
    /// </summary>
    public const string OnboardingStepNotFound = "ONBOARDING_STEP_NOT_FOUND";
}
