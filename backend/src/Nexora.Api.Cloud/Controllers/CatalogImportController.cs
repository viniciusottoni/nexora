using System.Diagnostics;
using Nexora.Api.Cloud.Infrastructure;
using Nexora.Application.Catalog.Import.Commands.ImportCatalog;
using Nexora.Application.Catalog.Import.Queries.GetCatalogImportTemplate;
using Nexora.Application.Catalog.Import.Queries.ValidateCatalogImport;
using Nexora.Contracts.Catalog;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Cloud.Controllers;

/// <summary>
/// Importação de cardápio por planilha (US-144) — carga inicial de categorias/produtos/variações/
/// preços a partir de um .xlsx, com pré-visualização obrigatória e importação incremental (upsert
/// por nome, ver <c>Nexora.Application.Catalog.Import.Shared.CatalogImportPlanner</c>). Mesma
/// policy <c>ProductWrite</c> ("catalog:write") do resto do CRUD de cardápio
/// (<see cref="ProductsController"/>/<see cref="CategoriesController"/>) — quem pode cadastrar um
/// produto manualmente pode importar em lote.
/// </summary>
[ApiController]
[Authorize]
[Route("v1/catalog/import")]
public sealed class CatalogImportController : ControllerBase
{
    /// <summary>Mesmo teto de <c>PrepareProductImageUploadCommandValidator</c> (US-010, 10 MB) — planilha de cardápio nunca chega perto disso.</summary>
    private const long MaxFileSizeBytes = 10_000_000;

    private readonly ISender _sender;

    public CatalogImportController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Modelo de planilha para download, com cabeçalho e linhas de exemplo preenchidas (US-144 §10).</summary>
    [HttpGet("template")]
    [Authorize(Policy = "ProductWrite")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadTemplate(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetCatalogImportTemplateQuery(), cancellationToken);
        if (result.IsFailure)
        {
            return result.ToActionResult(HttpContext);
        }

        var file = result.Value!;
        return File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>
    /// Valida a planilha enviada e devolve a pré-visualização (o que seria criado/atualizado) —
    /// NUNCA grava nada (US-144 §4, cenário "Pré-visualização"). Sempre 200: <c>valid=false</c>
    /// no corpo é o resultado esperado de uma planilha com erro, não uma falha HTTP (US-144 §7).
    /// </summary>
    [HttpPost("validate")]
    [Authorize(Policy = "ProductWrite")]
    [RequestSizeLimit(MaxFileSizeBytes)]
    [ProducesResponseType(typeof(CatalogImportValidateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Validate(
        IFormFile? file,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var upload = await ReadFileAsync(file);
        if (upload is null)
        {
            return BuildInvalidFileProblem("Envie um arquivo .xlsx de até 10 MB no modelo disponibilizado.");
        }

        var result = await _sender.Send(new ValidateCatalogImportQuery(upload.Value.Content, upload.Value.FileName), cancellationToken);

        if (result.IsFailure && result.Code == ApiErrorCodes.CatalogImportInvalidFile)
        {
            return BuildInvalidFileProblem(result.Error!);
        }

        return result.ToActionResult(HttpContext);
    }

    /// <summary>
    /// Confirma a importação — revalida a planilha do zero (nunca reaproveita o resultado de uma
    /// chamada anterior a <c>/validate</c>) e, se válida, cria/atualiza categorias, produtos,
    /// variações e preços em uma única transação. Linha inválida devolve 422 com os mesmos erros
    /// por linha do endpoint de validação, e nada é gravado (US-144 §4).
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "ProductWrite")]
    [RequestSizeLimit(MaxFileSizeBytes)]
    [ProducesResponseType(typeof(CatalogImportCommitResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(CatalogImportCommitResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Import(
        IFormFile? file,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var upload = await ReadFileAsync(file);
        if (upload is null)
        {
            return BuildInvalidFileProblem("Envie um arquivo .xlsx de até 10 MB no modelo disponibilizado.");
        }

        var result = await _sender.Send(new ImportCatalogCommand(upload.Value.Content, upload.Value.FileName), cancellationToken);

        if (result.IsFailure)
        {
            return result.Code == ApiErrorCodes.CatalogImportInvalidFile
                ? BuildInvalidFileProblem(result.Error!)
                : result.ToActionResult(HttpContext);
        }

        var response = result.Value!;
        if (!response.Valid)
        {
            // ver docstring de ImportCatalogCommandHandler: o handler sempre devolve
            // Result.Success — este 422 é decidido aqui, inspecionando o corpo, não pelo canal
            // genérico de Result.Failure/ResultExtensions.
            return UnprocessableEntity(response);
        }

        return StatusCode(StatusCodes.Status201Created, response);
    }

    private static async Task<(byte[] Content, string FileName)?> ReadFileAsync(IFormFile? file)
    {
        if (file is null || file.Length == 0 || file.Length > MaxFileSizeBytes)
        {
            return null;
        }

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        return (stream.ToArray(), file.FileName);
    }

    /// <summary>
    /// Monta um 400 no formato RFC 7807/ADR-021 (mesmas extensões de <c>ResultExtensions</c>:
    /// <c>code</c>/<c>recoverable</c>/<c>requiresAuthorization</c>/<c>traceId</c>) sem passar pelo
    /// canal genérico de <c>Result</c> — o arquivo é rejeitado ANTES de existir um <c>Result</c>
    /// (tamanho/extensão checados aqui no controller) ou o código
    /// <see cref="ApiErrorCodes.CatalogImportInvalidFile"/> ainda não está mapeado em
    /// <c>ResultExtensions.MapErrorCode</c> (ver docstring de <c>ApiErrorCodes.CatalogImport.cs</c>
    /// — arquivo não editado por esta tarefa).
    /// </summary>
    private IActionResult BuildInvalidFileProblem(string message)
    {
        var problem = new ProblemDetails
        {
            Type = "https://docs.nexora.app/errors/catalog-import-invalid-file",
            Title = message,
            Detail = message,
            Status = StatusCodes.Status400BadRequest,
            Instance = HttpContext.Request.Path.Value is { Length: > 0 } path ? path : "/",
        };
        problem.Extensions["code"] = ApiErrorCodes.CatalogImportInvalidFile;
        problem.Extensions["recoverable"] = true;
        problem.Extensions["requiresAuthorization"] = false;
        problem.Extensions["traceId"] = Activity.Current?.TraceId.ToHexString() ?? Guid.NewGuid().ToString("N");

        return new ObjectResult(problem) { StatusCode = StatusCodes.Status400BadRequest };
    }
}
