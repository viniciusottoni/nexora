namespace Awaken.Application.Common.Interfaces;

/// <summary>
/// US-236 — abstração de storage de mídia (bucket S3-compatível em produção, ADR-024) usada para publicar os GIFs
/// de execução em resolução 360 dos exercícios (RN-007: só após verificação da licença do EULA).
/// </summary>
public interface IMediaStorageService
{
    /// <summary>
    /// Envia o conteúdo para o storage de mídia e retorna a URL pública resultante.
    /// </summary>
    Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken cancellationToken = default);
}
