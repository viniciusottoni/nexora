using Nexora.Domain.Platform;

namespace Nexora.Application.Tenants.Support;

/// <summary>
/// US-156 · Recuperação do provisionamento e token de instalação — helper NOVO, extraído para
/// <see cref="GetTenantDeploymentStatusQueryHandler"/> não precisar duplicar, sem testar, a mesma
/// lógica de "completed/total/nextAction" que <c>GetTenantOverviewQueryHandler.BuildDeploymentAsync</c>
/// (US-152) já calcula inline. NÃO foi possível fazer o handler existente reusar este helper — a
/// disciplina de isolamento desta tarefa proíbe editar aquele arquivo em paralelo com outro agente
/// (ver relatório da tarefa) — então, por ora, as duas implementações ficam logicamente idênticas
/// mas fisicamente duplicadas; um follow-up de integração central pode substituir o corpo de
/// <c>BuildDeploymentAsync</c> por uma chamada a este helper sem mudar nenhum contrato observável.
/// </summary>
public static class OnboardingChecklistCalculator
{
    /// <summary>
    /// <paramref name="total"/> é sempre <see cref="Enum.GetValues{TEnum}"/> de
    /// <see cref="OnboardingStepKey"/> (9) — nunca a contagem de linhas persistidas, protegendo
    /// tenants semeados antes de todas as nove chaves existirem (mesma nota de
    /// <c>GetTenantOverviewQueryHandler.BuildDeploymentAsync</c>). <c>NextAction</c> é a chave, na
    /// ORDEM do enum, do primeiro passo cujo status não é <see cref="OnboardingStepStatus.Done"/>
    /// (linha ausente conta como <see cref="OnboardingStepStatus.Pending"/>); nulo quando
    /// <c>Completed == Total</c>.
    /// </summary>
    public static (int Completed, int Total, OnboardingStepKey? NextAction) Calculate(
        IReadOnlyCollection<OnboardingStep> steps)
    {
        var statusByKey = steps.ToDictionary(s => s.Key, s => s.Status);

        var total = Enum.GetValues<OnboardingStepKey>().Length;
        var completed = steps.Count(s => s.Status == OnboardingStepStatus.Done);

        OnboardingStepKey? nextAction = null;
        foreach (var key in Enum.GetValues<OnboardingStepKey>())
        {
            var status = statusByKey.TryGetValue(key, out var persistedStatus) ? persistedStatus : OnboardingStepStatus.Pending;

            if (status != OnboardingStepStatus.Done)
            {
                nextAction = key;
                break;
            }
        }

        return (completed, total, nextAction);
    }
}
