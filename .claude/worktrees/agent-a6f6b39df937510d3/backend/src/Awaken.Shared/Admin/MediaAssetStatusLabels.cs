namespace Awaken.Shared.Admin;

/// <summary>
/// US-222: vocabulário de status de mídia por exercício, usado em filtros e na resposta da
/// lista de diagnóstico de mídia/CDN.
/// </summary>
public static class MediaAssetStatusLabels
{
    /// <summary>Tem ao menos um asset (imagem, GIF ou vídeo) válido e nenhum link inválido.</summary>
    public const string Ok = "ok";

    /// <summary>Não tem nenhuma mídia cadastrada (RN-001: sem mídia mínima nem fallback).</summary>
    public const string Missing = "missing";

    /// <summary>Ao menos uma URL cadastrada retornou erro HTTP/timeout no HEAD (RN-002).</summary>
    public const string InvalidLink = "invalid_link";

    /// <summary>Asset válido, porém lento (RN-003) — acima do limiar de atenção de latência.</summary>
    public const string Slow = "slow";
}
