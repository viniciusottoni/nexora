namespace Nexora.Application.Abstractions.Platform;

/// <summary>
/// Versão do binário em execução (edge ou nuvem) — usada em respostas de saúde/sincronização
/// para o parque distribuído acompanhar quem está desatualizado (ADR-019). Implementado em
/// Infrastructure a partir da configuração/assembly, nunca lido de variável de ambiente
/// diretamente em Application.
/// </summary>
public interface IAppVersionProvider
{
    string CurrentVersion { get; }
}
