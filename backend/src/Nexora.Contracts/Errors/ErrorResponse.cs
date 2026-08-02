namespace Nexora.Contracts.Errors;

/// <summary>
/// <b>Legado — não é mais o corpo real da resposta.</b> Corpo cru vindo antes desta tarefa migrar
/// o mecanismo de erro para o <c>Microsoft.AspNetCore.Mvc.ProblemDetails</c>/
/// <c>ValidationProblemDetails</c> nativos (ver
/// <c>Nexora.Api.Edge</c>/<c>Api.Cloud.Infrastructure.ResultExtensions</c>) — <c>Nexora.Contracts</c>
/// não pode referenciar ASP.NET Core (ADR-039), então esse tipo não existe aqui. Este record
/// continua no código só como anotação de <c>[ProducesResponseType]</c> em alguns controllers
/// (Swagger); atualizá-los para <c>ProblemDetails</c> ficou fora do escopo desta tarefa de
/// plumbing (risco/esforço alto por tocar treze controllers só por precisão de documentação, sem
/// efeito no contrato real de runtime — ver relatório da tarefa). Não confiar neste shape para
/// nada além de metadado de OpenAPI.
/// </summary>
public sealed record ErrorResponse(
    string Message,
    string Code,
    IReadOnlyList<FieldErrorResponse> Errors,
    string TraceId,
    bool Recoverable = false,
    bool RequiresAuthorization = false);

public sealed record FieldErrorResponse(string Field, string Message);
