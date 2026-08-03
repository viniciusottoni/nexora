namespace Nexora.Application.Operation.Abstractions;

/// <summary>Uma mesa a imprimir — porta de entrada de <see cref="IQrCodePdfRenderer"/>.</summary>
/// <param name="TableId">Usado só para eventual depuração/observabilidade — nunca impresso no PDF.</param>
/// <param name="Label">Rótulo grande da mesa (US-020 §10: "PDF ... com rótulo grande da mesa, para conferência na hora de colar").</param>
/// <param name="AreaName">Nome do ambiente — dá contexto quando o gestor imprime várias áreas de uma vez.</param>
/// <param name="QrPayload">Conteúdo cru codificado no QR Code — o <c>qr_token</c> opaco da mesa.</param>
public sealed record TableQrCodePrintItem(Guid TableId, string Label, string AreaName, string QrPayload);

/// <summary>
/// Renderiza o PDF de QR Codes pronto para impressão (US-020 §4/§10: "um código por página,
/// identificado pelo rótulo da mesa"). Abstração em Application, implementação concreta em
/// Infrastructure (QRCoder + QuestPDF) — mesmo padrão de porta/adaptador de
/// <c>IBrandingStorage</c>/<c>IBackupStorage</c>: Application não pode referenciar bibliotecas de
/// renderização de imagem/documento diretamente (ADR-039).
/// </summary>
public interface IQrCodePdfRenderer
{
    /// <summary>Bytes do PDF — uma página por item, na mesma ordem recebida.</summary>
    byte[] Render(IReadOnlyList<TableQrCodePrintItem> items);
}
