using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Catalog;

namespace Nexora.Application.Catalog.Import.Queries.ValidateCatalogImport;

/// <summary>
/// Porta de <c>POST /v1/catalog/import/validate</c> (US-144 §7). Modelado como <see cref="IQuery{TResponse}"/>
/// mesmo a requisição HTTP sendo POST (corpo multipart não cabe em GET) — nunca passa por
/// <c>TransactionBehavior</c> (só query passa reto pelo pipeline), então nenhum <c>_db.Xxx.Add</c>
/// desta árvore de chamada pode sobreviver a um <c>SaveChangesAsync</c>: a garantia "nada é gravado
/// antes da confirmação" (US-144 §4, cenário "Pré-visualização") vem de construção, não de
/// disciplina do handler.
/// </summary>
public sealed record ValidateCatalogImportQuery(byte[] FileContent, string FileName) : IQuery<CatalogImportValidateResponse>;
