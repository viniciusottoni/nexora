namespace Nexora.Application.Abstractions.Catalog;

/// <summary>
/// Uma linha de dados da planilha de importação de cardápio (US-144). <see cref="RowNumber"/> é o
/// número REAL da linha na planilha (1-based, a primeira linha de dados é sempre 2 — a linha 1 é o
/// cabeçalho) — preservado até a resposta de erro, porque "aponta a linha e a coluna exatas" é a
/// exigência central da US (§10: "planilha grande com erro genérico é inutilizável"). As chaves de
/// <see cref="Cells"/> são o texto do cabeçalho normalizado (<c>Trim().ToLowerInvariant()</c>);
/// célula vazia vira <c>null</c>, nunca string vazia.
/// </summary>
public sealed record SpreadsheetRow(int RowNumber, IReadOnlyDictionary<string, string?> Cells);

/// <summary>
/// Tabela genérica de células lidas de uma planilha — só o suficiente para validação linha a linha;
/// nenhum tipo de <c>ClosedXML</c> (ou qualquer outra biblioteca concreta de formato de arquivo)
/// atravessa esta fronteira (ADR-039).
/// </summary>
public sealed record SpreadsheetTable(IReadOnlyList<string> Headers, IReadOnlyList<SpreadsheetRow> Rows);

/// <summary>
/// Porta de leitura/geração de planilha (US-144 — Importação de cardápio por planilha) —
/// implementada em <c>Nexora.Infrastructure.Catalog.ClosedXmlSpreadsheetParser</c> com ClosedXML.
/// Mesmo idioma de <see cref="Nexora.Application.Abstractions.Storage.IProductMediaStorage"/>:
/// Application só conhece a porta, nunca a biblioteca concreta.
/// </summary>
public interface ISpreadsheetParser
{
    /// <summary>
    /// Lê a primeira planilha do arquivo (linha 1 = cabeçalho). Linhas totalmente vazias (rodapé,
    /// espaço em branco deixado pelo usuário) são descartadas silenciosamente — não geram linha
    /// nem erro. Lança <see cref="InvalidOperationException"/> quando o arquivo não é um .xlsx
    /// válido (corrompido, formato errado, sem nenhuma planilha) — o chamador (handler de
    /// Application) converte para <c>ApiErrorCodes.CatalogImportInvalidFile</c>.
    /// </summary>
    SpreadsheetTable Parse(Stream fileStream);

    /// <summary>
    /// Gera um .xlsx com uma linha de cabeçalho (negrito) seguida das linhas de exemplo —
    /// <c>GET /v1/catalog/import/template</c> (US-144 §10: "modelo de planilha para download, com
    /// exemplos preenchidos").
    /// </summary>
    byte[] BuildTemplate(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> exampleRows, string sheetName);
}
