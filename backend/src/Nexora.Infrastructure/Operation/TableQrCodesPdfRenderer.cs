using Nexora.Application.Operation.Abstractions;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Nexora.Infrastructure.Operation;

/// <summary>
/// Renderiza o PDF de QR Codes pronto para impressão (US-020, cenário Gherkin "Exportação para
/// impressão": "um código por página, identificado pelo rótulo da mesa"). QRCoder gera a matriz
/// do QR Code em PNG; QuestPDF monta o documento, uma página A4 por mesa, com o rótulo em
/// destaque tipográfico para conferência rápida na hora de colar na mesa (US-020 §10).
/// </summary>
public sealed class TableQrCodesPdfRenderer : IQrCodePdfRenderer
{
    static TableQrCodesPdfRenderer()
    {
        // QuestPDF exige a licença declarada uma única vez por processo antes do primeiro
        // GeneratePdf(). Community: gratuita para empresas/times pequenos (ver termos da
        // QuestPDF) — adequado ao estágio atual do produto; reavaliar se/quando isso mudar.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Render(IReadOnlyList<TableQrCodePrintItem> items)
    {
        using var qrGenerator = new QRCodeGenerator();

        var document = Document.Create(container =>
        {
            foreach (var item in items)
            {
                var qrPngBytes = RenderQrPng(qrGenerator, item.QrPayload);

                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.Content().Column(column =>
                    {
                        column.Spacing(16);

                        column.Item().AlignCenter().Text(item.AreaName)
                            .FontSize(16).FontColor(Colors.Grey.Darken1);

                        column.Item().AlignCenter().Text($"Mesa {item.Label}")
                            .FontSize(48).Bold();

                        column.Item().AlignCenter().MaxWidth(320).Image(qrPngBytes);

                        column.Item().AlignCenter().Text("Aponte a câmera para pedir da mesa")
                            .FontSize(11).FontColor(Colors.Grey.Darken1);
                    });
                });
            }
        });

        return document.GeneratePdf();
    }

    private static byte[] RenderQrPng(QRCodeGenerator qrGenerator, string payload)
    {
        using var qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        using var pngQrCode = new PngByteQRCode(qrCodeData);
        return pngQrCode.GetGraphic(20);
    }
}
