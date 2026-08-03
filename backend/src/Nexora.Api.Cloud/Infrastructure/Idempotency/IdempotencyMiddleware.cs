using System.Diagnostics;
using System.Security.Cryptography;
using Nexora.Application.Abstractions.Idempotency;
using Nexora.Application.Abstractions.Security;
using Nexora.Contracts.Http;
using Nexora.Shared.Errors;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Cloud.Infrastructure.Idempotency;

/// <summary>
/// Middleware genérico de idempotência de escrita (ADR-020) — registrado uma única vez em
/// <c>Program.cs</c>, cobre POST/PUT/PATCH/DELETE em vez de cada controller ler e ignorar o
/// header <c>Idempotency-Key</c> (o gap original: TODO endpoint declarava
/// <c>[FromHeader(Name = "Idempotency-Key")] string? idempotencyKey</c> e nunca usava o valor).
/// Gêmeo do <c>Nexora.Api.Edge.Infrastructure.Idempotency.IdempotencyMiddleware</c> — mantenha os
/// dois consistentes.
/// </summary>
/// <remarks>
/// <para>
/// <b>Escopo da chave — decisão de design:</b> a chave é GLOBAL (PK isolada em
/// <c>idempotency_key</c>, sem <c>tenant_id</c> na chave composta), não escopada por
/// tenant+usuário. Duas razões: (1) o middleware roda para rotas que ainda não têm tenant
/// resolvido quando a requisição chega — provisionamento de tenant (ação de plataforma, tenant
/// nem existe ainda), registro/consumo de token de instalação e aceite de convite de dono (todos
/// <c>[AllowAnonymous]</c> por design, a credencial é o token no corpo/header, não uma sessão) —
/// escopar por tenant tornaria a chave inutilizável exatamente nesses casos; (2) a chave já É
/// globalmente única por construção (o cliente gera um UUID aleatório por intenção — ver
/// apps/web-platform/src/features/tenants/tenants-api.ts, apps/web-admin/*-api.ts), então uma
/// colisão entre tenants diferentes é astronomicamente improvável, e o par
/// <c>endpoint + requestHash</c> gravado junto detecta e rejeita (422 IDEMPOTENCY_KEY_REUSED)
/// qualquer reuso — mesmo entre tenants — cujo destino ou corpo não bata com a primeira chamada.
/// <c>tenant_id</c> continua gravado como metadado (auditoria/depuração), só não faz parte da
/// identidade da linha.
/// </para>
/// <para>
/// <b>Rotas isentas:</b> ver <see cref="IdempotencyExemptAttribute"/> — login por senha
/// (<c>POST /v1/auth/login</c>), refresh (<c>POST /v1/auth/refresh</c>) e upload de backup
/// (<c>PUT /v1/platform/installations/{id}/backups</c>, corpo de até 1&#160;GB já validado pelo
/// próprio hash de conteúdo em <c>UploadBackupCommandHandler</c>) neste projeto.
/// </para>
/// </remarks>
public sealed class IdempotencyMiddleware
{
    private static readonly HashSet<string> WriteMethods =
        new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH", "DELETE" };

    private static readonly TimeSpan Ttl = TimeSpan.FromHours(24); // ADR-020: "armazena... por 24 horas"

    private const string HeaderName = "Idempotency-Key";
    private const string ReplayHeaderName = "Idempotent-Replay";

    private readonly RequestDelegate _next;

    public IdempotencyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IIdempotencyStore store, ICurrentTenantContext tenantContext)
    {
        if (!WriteMethods.Contains(context.Request.Method))
        {
            await _next(context);
            return;
        }

        if (context.GetEndpoint()?.Metadata.GetMetadata<IdempotencyExemptAttribute>() is not null)
        {
            await _next(context);
            return;
        }

        var key = context.Request.Headers[HeaderName].ToString();
        if (string.IsNullOrWhiteSpace(key))
        {
            await WriteProblemAsync(
                context,
                StatusCodes.Status422UnprocessableEntity,
                ApiErrorCodes.IdempotencyKeyRequired,
                "O cabeçalho Idempotency-Key é obrigatório para esta operação (ADR-020).",
                recoverable: true);
            return;
        }

        var endpointDescriptor = $"{context.Request.Method} {context.Request.Path}";
        var requestHash = await ComputeRequestHashAsync(context);
        var cancellationToken = context.RequestAborted;

        var existing = await store.FindAsync(key, cancellationToken);
        if (existing is not null)
        {
            var sameIntent = existing.Endpoint == endpointDescriptor && existing.RequestHash == requestHash;
            if (!sameIntent)
            {
                // Mesma chave, requisição diferente — ADR-020: "indica erro do cliente", nunca
                // silencioso (a chave é a promessa de que é a MESMA intenção reenviada).
                await WriteProblemAsync(
                    context,
                    StatusCodes.Status422UnprocessableEntity,
                    ApiErrorCodes.IdempotencyKeyReused,
                    "Esta chave de idempotência já foi usada para uma requisição diferente.",
                    recoverable: false);
                return;
            }

            if (existing.IsCompleted)
            {
                await ReplayAsync(context, existing.ResponseStatus ?? StatusCodes.Status200OK, existing.ResponseBody);
                return;
            }

            if (existing.IsInProgress && !existing.IsExpired(DateTimeOffset.UtcNow))
            {
                // Mesma intenção, ainda IN_PROGRESS e dentro da validade — outra requisição (ou a
                // mesma, em paralelo) está processando agora. ADR-020: 409, cliente reenvia com
                // backoff.
                await WriteProblemAsync(
                    context,
                    StatusCodes.Status409Conflict,
                    ApiErrorCodes.RequestInProgress,
                    "Esta requisição já está em processamento.",
                    recoverable: true);
                return;
            }
            // FAILED (requisição anterior deu 5xx, ver DiscardAsync — reclama imediatamente, sem
            // esperar a validade original) ou IN_PROGRESS expirada (ADR-020 "trata como
            // requisição nova") -> cai para o BeginAsync abaixo, cujo
            // INSERT ... ON CONFLICT DO UPDATE reclama a linha.
        }

        var outcome = await store.BeginAsync(
            key, tenantContext.TenantId, endpointDescriptor, requestHash, DateTimeOffset.UtcNow.Add(Ttl), cancellationToken);
        if (outcome == IdempotencyBeginOutcome.AlreadyReserved)
        {
            await WriteProblemAsync(
                context,
                StatusCodes.Status409Conflict,
                ApiErrorCodes.RequestInProgress,
                "Esta requisição já está em processamento.",
                recoverable: true);
            return;
        }

        await ExecuteAndStoreAsync(context, store, key, cancellationToken);
    }

    private async Task ExecuteAndStoreAsync(HttpContext context, IIdempotencyStore store, string key, CancellationToken cancellationToken)
    {
        var originalBody = context.Response.Body;
        using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        Exception? pipelineException = null;
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            pipelineException = ex;
        }
        finally
        {
            context.Response.Body = originalBody;
        }

        if (pipelineException is not null)
        {
            // Exceção propagada antes de qualquer resposta ser produzida (ou de o
            // ExceptionHandlerMiddleware, registrado mais externamente, tratá-la) — ADR-020:
            // falha (o pior caso de 5xx) não armazena, libera a chave para um reenvio real.
            await store.DiscardAsync(key, CancellationToken.None);
            throw pipelineException;
        }

        var responseBytes = buffer.ToArray();
        var responseText = responseBytes.Length == 0 ? null : System.Text.Encoding.UTF8.GetString(responseBytes);

        // Finaliza a reserva antes de enviar a resposta ao cliente. Se o navegador fechar a
        // conexÃ£o durante o WriteAsync, o efeito de negÃ³cio jÃ¡ executado nÃ£o pode deixar a chave
        // presa como IN_PROGRESS por 24 horas.
        if (context.Response.StatusCode >= 500)
        {
            await store.DiscardAsync(key, CancellationToken.None);
        }
        else
        {
            await store.CompleteAsync(key, context.Response.StatusCode, responseText, CancellationToken.None);
        }

        // Cabeçalhos precisam ser definidos antes do primeiro byte do corpo ser enviado.
        // Definir Idempotent-Replay depois de WriteAsync fazia o Kestrel abortar a conexão,
        // embora o comando e o commit no banco já tivessem sido concluídos.
        context.Response.Headers[ReplayHeaderName] = "false";
        await originalBody.WriteAsync(responseBytes, cancellationToken);
    }

    private static async Task ReplayAsync(HttpContext context, int statusCode, string? responseBody)
    {
        context.Response.StatusCode = statusCode;
        context.Response.Headers[ReplayHeaderName] = "true"; // ADR-020, contrato: "Idempotent-Replay: true"
        if (string.IsNullOrEmpty(responseBody))
        {
            return;
        }

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(responseBody, context.RequestAborted);
    }

    /// <summary>
    /// Corpo cru (hash) mais método+caminho (comparado separadamente como <c>Endpoint</c>) é a
    /// definição de "mesma intenção" (ADR-020). Limitação conhecida: dois corpos JSON
    /// semanticamente iguais mas com bytes diferentes (ordem de campo, espaçamento) contam como
    /// "diferentes" — mesma limitação do pseudocódigo de referência do ADR (hash do corpo bruto).
    /// </summary>
    private static async Task<string> ComputeRequestHashAsync(HttpContext context)
    {
        var request = context.Request;
        request.EnableBuffering();
        request.Body.Position = 0;
        var hash = await SHA256.HashDataAsync(request.Body, context.RequestAborted);
        request.Body.Position = 0;
        return Convert.ToHexString(hash);
    }

    private static async Task WriteProblemAsync(
        HttpContext context, int status, string code, string message, bool recoverable)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Type = $"https://docs.nexora.app/errors/{code.ToLowerInvariant().Replace('_', '-')}",
            Title = message,
            Detail = message,
            Instance = context.Request.Path.Value is { Length: > 0 } path ? path : "/",
        };
        problem.Extensions["code"] = code;
        problem.Extensions["recoverable"] = recoverable;
        problem.Extensions["requiresAuthorization"] = false;
        problem.Extensions["traceId"] = Activity.Current?.TraceId.ToHexString() ?? Guid.NewGuid().ToString("N");

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        // ProblemDetails.Extensions é [JsonExtensionData] — System.Text.Json já flatteniza para
        // propriedades de topo sem precisar de IProblemDetailsService (mesma serialização que
        // ResultExtensions.BuildErrorResult usa ao devolver ObjectResult(problem) de um controller).
        await context.Response.WriteAsJsonAsync(problem, context.RequestAborted);
    }
}
