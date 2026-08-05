namespace Nexora.Shared.Errors;

/// <summary>
/// Códigos de erro da importação de cardápio por planilha (US-144, ADR-021).
/// </summary>
/// <remarks>
/// Mapeamento a incluir em <c>Nexora.Api.Cloud/Infrastructure/ResultExtensions.cs</c> (e no gêmeo
/// de <c>Nexora.Api.Edge</c>, se algum dia esse app expuser o mesmo endpoint) — não editado por
/// esta tarefa (ver docstring "não editar em paralelo" do arquivo); reportado no relatório da
/// tarefa:
/// <list type="bullet">
/// <item><see cref="CatalogImportInvalidFile"/> -&gt; 400 Bad Request, recoverable=true, requiresAuthorization=false.</item>
/// <item><see cref="CatalogImportValidationFailed"/> -&gt; 422 Unprocessable Entity, recoverable=true, requiresAuthorization=false.</item>
/// </list>
/// Na prática, nenhum dos dois HOJE depende desse switch: <see cref="CatalogImportInvalidFile"/> é
/// interceptado por <c>CatalogImportController</c> antes de <c>ToActionResult</c> (constrói o
/// <c>ProblemDetails</c> 400 localmente, ver <c>CatalogImportController.BuildInvalidFileProblem</c>),
/// e <see cref="CatalogImportValidationFailed"/> nem chega a virar <c>Result.Failure</c> — o
/// commit (<c>POST /v1/catalog/import</c>) devolve <c>Result.Success</c> com um corpo que carrega
/// <c>valid=false</c> e a lista de erros por linha, e o controller decide 201 vs 422 inspecionando
/// esse campo (ver docstring de <c>ImportCatalogCommandHandler</c> para o porquê). Os dois códigos
/// continuam catalogados aqui para documentação/Swagger e para o dia em que alguém preferir migrar
/// esse fluxo para o canal genérico de <c>Result</c>.
/// </remarks>
public static partial class ApiErrorCodes
{
    /// <summary>Arquivo enviado (<c>POST /v1/catalog/import/validate</c> ou <c>/v1/catalog/import</c>) não é uma planilha .xlsx válida, está vazio, corrompido ou excede o tamanho máximo permitido.</summary>
    public const string CatalogImportInvalidFile = "CATALOG_IMPORT_INVALID_FILE";

    /// <summary><c>POST /v1/catalog/import</c> com uma ou mais linhas inválidas — nada foi gravado (US-144 §4, cenário "Erros por linha").</summary>
    public const string CatalogImportValidationFailed = "CATALOG_IMPORT_VALIDATION_FAILED";
}
