namespace Nexora.Application.Installations.Support;

/// <summary>
/// US-146 §4, cenário "Instalação com pendência de sincronização" — decide se o volume de eventos
/// ainda não sincronizados (<c>Outbox</c> com <c>Status != "SYNCED"</c>, mesma contagem já usada por
/// <c>PollSyncHealthCommandHandler</c>/<c>ListPlatformInstallationsQueryHandler</c>) é alto o
/// suficiente para ADIAR a atualização em vez de prosseguir. Sem estado, sem I/O — testável
/// isoladamente, mesmo idioma de <c>InstallationHealthClassifier</c>.
/// </summary>
/// <remarks>
/// Limiar é configuração GLOBAL do produto (ADR-013: proibido condicional por tenant), não um
/// valor por cliente. 500 eventos pendentes é uma escolha conservadora: bem acima do que uma loja
/// acumula numa janela normal de operação (dezenas a poucas centenas por turno), mas baixo o
/// suficiente para pegar o caso real que a US descreve — uma loja que ficou offline por horas e
/// tem uma fila grande ainda por esvaziar quando a janela de atualização chega. Atualizar (e
/// reiniciar containers) nesse momento arriscaria perder o outbox local antes de sincronizar.
/// </remarks>
public static class EdgeUpdateSyncPendingPolicy
{
    public const int PendingEventsThreshold = 500;

    public static bool ShouldDefer(int pendingEvents) => pendingEvents >= PendingEventsThreshold;
}
