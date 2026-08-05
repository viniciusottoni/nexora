using Nexora.Application.Abstractions.Messaging;

namespace Nexora.Application.Catalog.Import.Queries.GetCatalogImportTemplate;

/// <summary>Arquivo .xlsx gerado como modelo de importação — porta de <c>GET /v1/catalog/import/template</c>.</summary>
public sealed record CatalogImportTemplateFile(byte[] Content, string FileName, string ContentType);

/// <summary>
/// Pede o modelo de planilha de importação de cardápio (US-144 §10: "modelo de planilha para
/// download, com exemplos preenchidos"). Não depende de tenant/autenticação nenhuma além da policy
/// de autorização do controller — o modelo é sempre o mesmo, então nem precisa tocar o banco.
/// </summary>
public sealed record GetCatalogImportTemplateQuery : IQuery<CatalogImportTemplateFile>;
