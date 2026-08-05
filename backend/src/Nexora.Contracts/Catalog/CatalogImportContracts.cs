namespace Nexora.Contracts.Catalog;

/// <summary>
/// Erro de uma linha específica da planilha de importação (US-144 §10: "erros apontando linha e
/// coluna exatas — planilha grande com erro genérico é inutilizável"). <see cref="Row"/> é o número
/// real da linha na planilha (a primeira linha de dados é 2, já que 1 é o cabeçalho).
/// </summary>
public sealed record CatalogImportRowError(int Row, string Column, string Message);

/// <summary>Contagem por tipo de objeto do cardápio (US-144 §7) — usada tanto na pré-visualização quanto no resultado final.</summary>
public sealed record CatalogImportCounts(int Categories, int Products, int Variants);

/// <summary>
/// O que a importação criaria e o que atualizaria, sem gravar nada (US-144 §4, cenário
/// "Pré-visualização"). Categorias nunca aparecem em <see cref="ToUpdate"/> — esta história não
/// atualiza campo nenhum de categoria já existente, só reaproveita pelo nome (ver
/// <c>CatalogImportPlanner</c>).
/// </summary>
public sealed record CatalogImportPreview(CatalogImportCounts ToCreate, CatalogImportCounts ToUpdate);

/// <summary>Corpo de <c>POST /v1/catalog/import/validate</c> — nunca grava nada (US-144 §7, §3.1 "validação com relatório de erros por linha").</summary>
public sealed record CatalogImportValidateResponse(
    bool Valid,
    IReadOnlyList<CatalogImportRowError> Errors,
    CatalogImportPreview Preview);

/// <summary>
/// Corpo de <c>POST /v1/catalog/import</c>. <see cref="Valid"/> falso significa que NENHUMA linha
/// foi gravada (US-144 §4, cenário "Erros por linha": "nenhuma linha deve ser importada até a
/// correção") — o controller usa este campo para decidir 201 (sucesso, contagens preenchidas) vs
/// 422 (linhas inválidas, <see cref="Errors"/> preenchido e as três contagens zeradas), sem passar
/// pelo canal genérico de erro de <c>Result</c> (ver <c>CatalogImportController</c> e a docstring de
/// <c>ImportCatalogCommandHandler</c>).
/// </summary>
public sealed record CatalogImportCommitResponse(
    bool Valid,
    IReadOnlyList<CatalogImportRowError> Errors,
    CatalogImportCounts Created,
    CatalogImportCounts Updated,
    int Skipped);
