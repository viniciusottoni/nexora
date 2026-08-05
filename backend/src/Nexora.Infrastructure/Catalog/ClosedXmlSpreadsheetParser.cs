using System.Globalization;
using ClosedXML.Excel;
using Nexora.Application.Abstractions.Catalog;

namespace Nexora.Infrastructure.Catalog;

/// <summary>
/// Implementação de <see cref="ISpreadsheetParser"/> com ClosedXML (US-144) — único ponto do
/// código que conhece o formato .xlsx concreto (ADR-039: Application só referencia a porta).
/// </summary>
public sealed class ClosedXmlSpreadsheetParser : ISpreadsheetParser
{
    public SpreadsheetTable Parse(Stream fileStream)
    {
        using var workbook = OpenWorkbook(fileStream);
        var worksheet = workbook.Worksheets.FirstOrDefault()
            ?? throw new InvalidOperationException("A planilha não contém nenhuma aba.");

        var usedRange = worksheet.RangeUsed();
        if (usedRange is null)
        {
            return new SpreadsheetTable(Array.Empty<string>(), Array.Empty<SpreadsheetRow>());
        }

        var firstRow = usedRange.FirstRow();
        var headerRowNumber = firstRow.RowNumber();
        var lastColumn = usedRange.LastColumn().ColumnNumber();

        var headers = new List<string>();
        for (var col = usedRange.FirstColumn().ColumnNumber(); col <= lastColumn; col++)
        {
            headers.Add(worksheet.Cell(headerRowNumber, col).GetString().Trim().ToLowerInvariant());
        }

        var rows = new List<SpreadsheetRow>();
        var lastRowNumber = usedRange.LastRow().RowNumber();
        for (var rowNumber = headerRowNumber + 1; rowNumber <= lastRowNumber; rowNumber++)
        {
            var cells = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            var hasAnyValue = false;

            for (var i = 0; i < headers.Count; i++)
            {
                var header = headers[i];
                if (string.IsNullOrEmpty(header))
                {
                    continue;
                }

                var cell = worksheet.Cell(rowNumber, usedRange.FirstColumn().ColumnNumber() + i);
                var raw = FormatCellValue(cell);
                if (!string.IsNullOrEmpty(raw))
                {
                    hasAnyValue = true;
                }

                cells[header] = string.IsNullOrEmpty(raw) ? null : raw;
            }

            // Linha totalmente vazia (rodapé, espaço deixado pelo usuário no fim da planilha) —
            // descartada silenciosamente, nunca vira erro (US-144 §15: modelo tem que ser tolerante
            // ao mínimo de ruído possível, senão o cliente desiste).
            if (!hasAnyValue)
            {
                continue;
            }

            rows.Add(new SpreadsheetRow(rowNumber, cells));
        }

        return new SpreadsheetTable(headers, rows);
    }

    public byte[] BuildTemplate(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> exampleRows, string sheetName)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(sheetName);

        for (var col = 0; col < headers.Count; col++)
        {
            var cell = worksheet.Cell(1, col + 1);
            cell.Value = headers[col];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(230, 230, 230);
        }

        for (var r = 0; r < exampleRows.Count; r++)
        {
            var exampleRow = exampleRows[r];
            for (var col = 0; col < exampleRow.Count; col++)
            {
                worksheet.Cell(r + 2, col + 1).Value = exampleRow[col];
            }
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// <see cref="IXLCell.GetString"/> formata células numéricas com base no número/cultura da
    /// MÁQUINA que roda o processo (arriscado num servidor cujo SO pode estar em pt-BR, o que
    /// trocaria o ponto decimal por vírgula silenciosamente) — para célula numérica, lê o valor
    /// tipado e formata sempre com <see cref="CultureInfo.InvariantCulture"/>; célula de texto
    /// (o usuário digitou "45.90" direto) passa por <see cref="IXLCell.GetString"/> normalmente,
    /// já que nesse caso o texto já É o que o usuário digitou, sem formatação de número envolvida.
    /// </summary>
    private static string? FormatCellValue(IXLCell cell)
    {
        if (cell.IsEmpty())
        {
            return null;
        }

        if (cell.DataType == XLDataType.Number)
        {
            // Conversão double -> decimal do .NET arredonda para 15 dígitos significativos,
            // eliminando o ruído de ponto flutuante binário (ex.: 45.9 nunca vira
            // 45.899999999999999) — seguro para preço de cardápio (ADR-017).
            return ((decimal)cell.GetDouble()).ToString(CultureInfo.InvariantCulture);
        }

        return cell.GetString().Trim();
    }

    /// <summary>
    /// Isola a chamada de construção do <see cref="XLWorkbook"/> — ClosedXML lança tipos próprios
    /// (<c>ArgumentException</c>/<c>Exception</c> genéricos, dependendo do defeito) para arquivo
    /// corrompido ou que não é .xlsx; normalizado aqui para <see cref="InvalidOperationException"/>,
    /// o único tipo que os handlers de Application precisam capturar (ver docstring de
    /// <see cref="ISpreadsheetParser.Parse"/>).
    /// </summary>
    private static XLWorkbook OpenWorkbook(Stream fileStream)
    {
        try
        {
            return new XLWorkbook(fileStream);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            throw new InvalidOperationException("Não foi possível abrir o arquivo como planilha .xlsx.", ex);
        }
    }
}
