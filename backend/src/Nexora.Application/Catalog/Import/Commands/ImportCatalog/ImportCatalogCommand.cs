using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Catalog;

namespace Nexora.Application.Catalog.Import.Commands.ImportCatalog;

/// <summary>
/// Porta de <c>POST /v1/catalog/import</c> (US-144 §7). <see cref="ICommand{TResponse}"/> de
/// verdade (passa por <c>TransactionBehavior</c>) — mas o handler NUNCA devolve
/// <c>Result.Failure</c> por linha inválida (ver docstring de <c>ImportCatalogCommandHandler</c>):
/// o commit sempre "sucede" no sentido de <c>Result</c>, e o corpo devolvido é que carrega
/// <c>Valid=false</c> quando a planilha tinha erro. Isso mantém a garantia do ADR-006/ADR-020 (uma
/// falha de negócio comum devolveria <c>Result.Failure</c> e o <c>TransactionBehavior</c> faria
/// rollback) e evita forçar uma lista de erros por linha dentro do canal genérico
/// <c>IReadOnlyDictionary&lt;string,string[]&gt;</c> de <c>Result.Errors</c>, que não modela bem
/// "vários erros, cada um com linha+coluna+mensagem".
/// </summary>
public sealed record ImportCatalogCommand(byte[] FileContent, string FileName) : ICommand<CatalogImportCommitResponse>;
