using System.Diagnostics;
using System.Globalization;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Shared.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Cloud.Infrastructure;

/// <summary>
/// Traduz <see cref="Result"/>/<see cref="Result{T}"/> em resposta HTTP RFC 7807 (ADR-021), usando
/// o <see cref="ProblemDetails"/>/<see cref="ValidationProblemDetails"/> nativos do ASP.NET Core —
/// não mais o record próprio <c>Nexora.Contracts.Errors.ErrorResponse</c> (esse continua existindo
/// só para anotação de Swagger nos controllers; o corpo real da resposta é o que este arquivo
/// monta). Espelha o ResultExtensions do seminarioteologico. NOTA: este arquivo é reescrito/
/// estendido centralmente conforme novos códigos de erro são adicionados pelos módulos — não
/// editar em paralelo por múltiplos agentes; adicione o código em
/// Shared/Errors/ApiErrorCodes.&lt;Modulo&gt;.cs e sinalize para <see cref="MapErrorCode"/> ser
/// atualizado depois. Mantenha este arquivo consistente com o gêmeo de Nexora.Api.Edge — a única
/// diferença deliberada são os dois códigos legados (<c>AFFILIATE_LOOKUP_FAILED</c>/
/// <c>EXPIRED_RESET_TOKEN</c>) que só o cloud já tinha antes desta tarefa.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Base do campo <c>type</c> do RFC 7807 — mesmo formato do exemplo do ADR-021
    /// (<c>https://docs.&lt;plataforma&gt;/errors/...</c>). Domínio do PRODUTO, nunca do
    /// cliente-piloto (ADR-013/ADR-010) — não reintroduza o domínio do cliente-piloto aqui.
    /// </summary>
    private const string ProblemTypeBaseUri = "https://docs.nexora.app/errors/";

    public static IActionResult ToActionResult<T>(this Result<T> result, HttpContext httpContext)
    {
        if (result.IsSuccess)
        {
            return new OkObjectResult(result.Value);
        }

        return BuildErrorResult(result.Code, result.Error!, result.Errors, httpContext);
    }

    public static IActionResult ToActionResult(this Result result, HttpContext httpContext)
    {
        if (result.IsSuccess)
        {
            return new NoContentResult();
        }

        return BuildErrorResult(result.Code, result.Error!, result.Errors, httpContext);
    }

    /// <summary>
    /// Callback de <c>AddProblemDetails(options => options.CustomizeProblemDetails = ...)</c>
    /// (Program.cs) — cobre os <see cref="ProblemDetails"/> que o PRÓPRIO framework monta sem
    /// passar por um <see cref="Result"/> nosso: 401 do desafio JWT, 403 de policy de autorização,
    /// 404 de rota sem match e 500 de exceção não tratada (<c>UseExceptionHandler</c>). Sem este
    /// hook, essas respostas teriam <c>type</c>/<c>title</c>/<c>status</c>/<c>detail</c> (o
    /// framework já produz isso desde o .NET 8) mas NUNCA as extensões
    /// <c>code</c>/<c>recoverable</c>/<c>requiresAuthorization</c>/<c>traceId</c> que o ADR-021 e
    /// o contrato do frontend (packages/contracts/src/errors.ts) exigem.
    /// </summary>
    public static void EnrichFrameworkProblemDetails(ProblemDetailsContext context)
    {
        var problem = context.ProblemDetails;
        var status = problem.Status ?? context.HttpContext.Response.StatusCode;
        var code = DefaultCodeForStatus(status);
        var (_, recoverable, requiresAuthorization) = MapErrorCode(code);

        problem.Type ??= BuildTypeUri(code);
        problem.Title ??= "Erro";
        problem.Detail ??= problem.Title;
        problem.Instance ??= ResolveInstance(context.HttpContext);
        problem.Extensions["code"] = code;
        problem.Extensions["recoverable"] = recoverable;
        problem.Extensions["requiresAuthorization"] = requiresAuthorization;
        problem.Extensions["traceId"] = ResolveTraceId();
    }

    private static ObjectResult BuildErrorResult(
        string? code,
        string message,
        IReadOnlyDictionary<string, string[]>? fieldErrors,
        HttpContext httpContext)
    {
        var effectiveCode = code ?? ApiErrorCodes.UnknownError;
        var (status, recoverable, requiresAuthorization) = MapErrorCode(effectiveCode);
        var meta = ExtractMeta(ref fieldErrors);
        var pendingItemsMeta = ExtractPendingItemsMeta(ref fieldErrors);
        if (pendingItemsMeta is { Count: > 0 })
        {
            meta ??= new Dictionary<string, object>();
            foreach (var (metaKey, metaValue) in pendingItemsMeta)
            {
                meta[metaKey] = metaValue;
            }
        }

        ProblemDetails problem = fieldErrors is { Count: > 0 }
            ? new ValidationProblemDetails(ToMutableErrors(fieldErrors)) { Status = status }
            : new ProblemDetails { Status = status };

        problem.Type = BuildTypeUri(effectiveCode);
        // Result só carrega uma mensagem (Error) — sem um par título curto/detalhe longo separado
        // como no exemplo do ADR-021. Simplificação deliberada: os dois campos recebem a mesma
        // mensagem em português (o frontend já exibe `detail` ao usuário final, ADR-021 §Formato).
        problem.Title = message;
        problem.Detail = message;
        problem.Instance = ResolveInstance(httpContext);
        problem.Extensions["code"] = effectiveCode;
        problem.Extensions["recoverable"] = recoverable;
        problem.Extensions["requiresAuthorization"] = requiresAuthorization;
        problem.Extensions["traceId"] = ResolveTraceId();
        if (meta is { Count: > 0 })
        {
            problem.Extensions["meta"] = meta;
        }

        return new ObjectResult(problem) { StatusCode = status };
    }

    /// <summary>
    /// Extrai a convenção histórica de "retryAfterSeconds dentro de Errors" (usada por
    /// LoginWithPin/LoginWithPassword/AuthorizeSensitiveAction, de antes deste contrato ter um
    /// campo <c>meta</c> de verdade) para <c>Extensions["meta"]["retryAfterSeconds"]</c> — formato
    /// que o frontend já lê (packages/ui/src/auth/operational-auth-client.ts,
    /// <c>problem.meta?.retryAfterSeconds</c>). Não exige alterar nenhum handler: a conversão
    /// acontece só aqui, no mapeamento genérico.
    /// </summary>
    private static Dictionary<string, object>? ExtractMeta(ref IReadOnlyDictionary<string, string[]>? fieldErrors)
    {
        const string retryAfterKey = "retryAfterSeconds";

        if (fieldErrors is null || !fieldErrors.TryGetValue(retryAfterKey, out var values) || values.Length == 0)
        {
            return null;
        }

        Dictionary<string, object>? meta = null;
        if (int.TryParse(values[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds))
        {
            meta = new Dictionary<string, object> { [retryAfterKey] = seconds };
        }

        fieldErrors = fieldErrors.Count == 1
            ? null
            : fieldErrors.Where(kv => kv.Key != retryAfterKey).ToDictionary(kv => kv.Key, kv => kv.Value);

        return meta;
    }

    /// <summary>
    /// US-035 §7: 422 PENDING_ITEMS { meta: { pendingItems: [{ name, status }] } } — mesma
    /// convenção de <see cref="ExtractMeta"/> acima, mas o valor é o JSON já serializado da lista
    /// inteira (não um escalar simples), reidratado como <see cref="System.Text.Json.JsonElement"/>
    /// para sair como array de objetos na resposta, não como string. Método separado (em vez de
    /// estender <see cref="ExtractMeta"/>) para não editar em paralelo com outro agente naquele
    /// método específico (ver docstring desta classe: "não editar em paralelo").
    /// </summary>
    private static Dictionary<string, object>? ExtractPendingItemsMeta(ref IReadOnlyDictionary<string, string[]>? fieldErrors)
    {
        const string key = Nexora.Application.Tables.Support.PendingItemsClosePolicy.MetaErrorsKey;

        if (fieldErrors is null || !fieldErrors.TryGetValue(key, out var values) || values.Length == 0)
        {
            return null;
        }

        var meta = new Dictionary<string, object>
        {
            ["pendingItems"] = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(values[0]),
        };

        fieldErrors = fieldErrors.Count == 1
            ? null
            : fieldErrors.Where(kv => kv.Key != key).ToDictionary(kv => kv.Key, kv => kv.Value);

        return meta;
    }

    private static Dictionary<string, string[]> ToMutableErrors(IReadOnlyDictionary<string, string[]> fieldErrors) =>
        new(fieldErrors);

    private static string BuildTypeUri(string code) =>
        ProblemTypeBaseUri + code.ToLowerInvariant().Replace('_', '-');

    private static string ResolveInstance(HttpContext httpContext) =>
        httpContext.Request.Path.Value is { Length: > 0 } path ? path : "/";

    /// <summary>
    /// W3C Trace Context (ADR-022) — 32 hex chars, nunca <see cref="HttpContext.TraceIdentifier"/>
    /// (esse é um id interno do Kestrel, não o <c>traceId</c> de tracing distribuído). Fallback só
    /// para o caso extremo de nenhuma <see cref="Activity"/> corrente (ex.: chamada fora de uma
    /// requisição HTTP instrumentada) — ainda assim precisa ser hex32 para bater com
    /// <c>problemDetailsSchema</c> do frontend.
    /// </summary>
    private static string ResolveTraceId() =>
        Activity.Current?.TraceId.ToHexString() ?? Guid.NewGuid().ToString("N");

    private static string DefaultCodeForStatus(int status) => status switch
    {
        StatusCodes.Status401Unauthorized => ApiErrorCodes.Unauthorized,
        StatusCodes.Status403Forbidden => ApiErrorCodes.AuthPermissionDenied,
        StatusCodes.Status404NotFound => ApiErrorCodes.NotFound,
        StatusCodes.Status409Conflict => ApiErrorCodes.Conflict,
        StatusCodes.Status400BadRequest => ApiErrorCodes.ValidationError,
        _ => ApiErrorCodes.UnknownError,
    };

    /// <summary>
    /// Mapeamento explícito código -&gt; (status HTTP, recoverable, requiresAuthorization) —
    /// ADR-021. Cobre TODO código definido em <c>Nexora.Shared.Errors.ApiErrorCodes.*.cs</c>; não
    /// existe mais heurística de substring (o gap original: "TENANT_SLUG_ALREADY_TAKEN" caía em
    /// 400 porque só reconhecia sufixo "ALREADY_EXISTS"). Código não catalogado aqui cai no
    /// catch-all final (500, INTERNAL_ERROR implícito) — nunca em 400 silencioso.
    /// </summary>
    private static (int Status, bool Recoverable, bool RequiresAuthorization) MapErrorCode(string code) => code switch
    {
        ApiErrorCodes.UnknownError => (StatusCodes.Status500InternalServerError, true, false),
        ApiErrorCodes.ValidationError => (StatusCodes.Status400BadRequest, true, false),
        ApiErrorCodes.Unauthorized => (StatusCodes.Status401Unauthorized, false, false),
        ApiErrorCodes.NotFound => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.Conflict => (StatusCodes.Status409Conflict, true, false),

        // Idempotência — ADR-020.
        ApiErrorCodes.IdempotencyKeyRequired => (StatusCodes.Status422UnprocessableEntity, true, false),
        ApiErrorCodes.IdempotencyKeyReused => (StatusCodes.Status422UnprocessableEntity, false, false),
        ApiErrorCodes.RequestInProgress => (StatusCodes.Status409Conflict, true, false),

        // Multi-tenant — ADR-004.
        ApiErrorCodes.TenantContextMissing => (StatusCodes.Status403Forbidden, false, false),
        ApiErrorCodes.TenantInactive => (StatusCodes.Status401Unauthorized, false, false),

        // Auth — ADR-014/ADR-023.
        ApiErrorCodes.AuthInvalidCredentials => (StatusCodes.Status401Unauthorized, false, false),
        ApiErrorCodes.AuthDeviceNotRegistered => (StatusCodes.Status403Forbidden, false, false),
        ApiErrorCodes.AuthPinLocked => (StatusCodes.Status429TooManyRequests, true, false),
        ApiErrorCodes.AuthUserBlocked => (StatusCodes.Status401Unauthorized, false, false),
        ApiErrorCodes.AuthUserInactive => (StatusCodes.Status401Unauthorized, false, false),
        ApiErrorCodes.AuthPermissionDenied => (StatusCodes.Status403Forbidden, false, false),
        // "Ação sensível sem/expirado X-Authorization-Token" (ver relatório da tarefa): nenhum
        // handler valida esse header ainda (consumidor de elevação pontual é US futura de Orders,
        // fora de escopo) — este é o código existente mais próximo semanticamente (sessão
        // operacional sem tenant/loja/ator/dispositivo válido para pedir a autorização).
        ApiErrorCodes.AuthorizationContextInvalid => (StatusCodes.Status403Forbidden, false, true),
        // Família do exemplo do ADR-021 (AUTHORIZATION_REQUIRED -> abre diálogo de PIN do
        // gerente). US-004: agora produzido de verdade por IAuthorizationTokenValidator quando o
        // X-Authorization-Token está ausente/expirado/emitido para outra ação — deixou de ser
        // código morto (ver AuthorizationTokenValidatorTests).
        ApiErrorCodes.AuthorizationRequired => (StatusCodes.Status403Forbidden, false, true),
        // Sessão sem atividade há mais tempo que o timeout configurado (US-004, gap "encerramento
        // de sessão inativa configurável") — exige nova autenticação, não é "recoverable" no
        // sentido de repetir a MESMA requisição.
        ApiErrorCodes.AuthSessionIdleTimeout => (StatusCodes.Status401Unauthorized, false, false),

        // Backup (edge -> cloud) — ver módulo Backups.
        ApiErrorCodes.BackupHashMismatch => (StatusCodes.Status400BadRequest, true, false),
        ApiErrorCodes.BackupPermissionDenied => (StatusCodes.Status403Forbidden, false, false),

        // Branding.
        ApiErrorCodes.BrandingTenantNotFound => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.BrandingStorageUnavailable => (StatusCodes.Status503ServiceUnavailable, true, false),

        // Devices/pareamento.
        ApiErrorCodes.DevicePairingCodeInvalid => (StatusCodes.Status403Forbidden, false, false),
        ApiErrorCodes.DevicePairingCodeExpired => (StatusCodes.Status403Forbidden, false, false),
        ApiErrorCodes.DevicePairingCodeConsumed => (StatusCodes.Status403Forbidden, false, false),
        ApiErrorCodes.DevicePairingRateLimited => (StatusCodes.Status429TooManyRequests, true, false),
        ApiErrorCodes.DeviceNotFound => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.DeviceActorRequired => (StatusCodes.Status403Forbidden, false, false),
        ApiErrorCodes.DeviceStoreContextMissing => (StatusCodes.Status403Forbidden, false, false),
        ApiErrorCodes.DeviceMustBeRevokedBeforeDelete => (StatusCodes.Status422UnprocessableEntity, true, false),

        // Installation (edge/cloud).
        ApiErrorCodes.InstallationNotFound => (StatusCodes.Status404NotFound, false, false),
        // Token de instalação de uso único já consumido — 403 (era 409 por cair na heurística de
        // substring "CONFLICT"; não é conflito de concorrência, é token que não serve mais).
        ApiErrorCodes.InstallationTokenConflict => (StatusCodes.Status403Forbidden, false, false),
        // Mesma família do token consumido: expirado também não é "reenviável" (410-like), mas
        // mapeado a 403 para ficar consistente com o código de pareamento expirado acima.
        ApiErrorCodes.InstallationTokenExpired => (StatusCodes.Status403Forbidden, false, false),
        ApiErrorCodes.InstallationPublicKeyPermissionDenied => (StatusCodes.Status403Forbidden, false, false),
        ApiErrorCodes.InstallationSignatureInvalidCredentials => (StatusCodes.Status401Unauthorized, false, false),
        ApiErrorCodes.InstallationBootstrapConfigMissing => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.InstallationBootstrapVersionMismatch => (StatusCodes.Status422UnprocessableEntity, false, false),

        // Roles.
        ApiErrorCodes.RoleNotFound => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.RoleCodeAlreadyExists => (StatusCodes.Status409Conflict, true, false),
        ApiErrorCodes.RoleOwnerMustKeepFullAccess => (StatusCodes.Status422UnprocessableEntity, true, false),

        // Tenants.
        ApiErrorCodes.TenantNotFound => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.TenantSlugAlreadyTaken => (StatusCodes.Status422UnprocessableEntity, true, false),
        ApiErrorCodes.OwnerInviteInvalidCredentials => (StatusCodes.Status401Unauthorized, false, false),

        // Operação — ambientes e mesas do salão (US-020).
        ApiErrorCodes.AreaNotFound => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.AreaHasActiveTables => (StatusCodes.Status422UnprocessableEntity, true, false),
        ApiErrorCodes.TableNotFound => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.TableLabelAlreadyExists => (StatusCodes.Status409Conflict, true, false),
        ApiErrorCodes.TableHasSessionHistory => (StatusCodes.Status422UnprocessableEntity, true, false),
        ApiErrorCodes.TablesExportEmpty => (StatusCodes.Status422UnprocessableEntity, true, false),

        // Sessão de mesa (US-021/US-022) — autoridade do dado é o edge (só ele expõe os
        // controllers de escrita/leitura); mapeado aqui só para manter o catálogo idêntico ao
        // gêmeo de Nexora.Api.Edge, como a docstring desta classe pede.
        ApiErrorCodes.TableAlreadyOpen => (StatusCodes.Status409Conflict, true, false),
        ApiErrorCodes.InvalidTableToken => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.TableSessionNotFound => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.TableSessionNotOpen => (StatusCodes.Status422UnprocessableEntity, true, false),

        // Consumo da mesa em tempo real (US-024) e repetição de item (US-028) — autoridade do
        // dado é o edge (só ele expõe os controllers); mapeado aqui só para manter o catálogo
        // idêntico ao gêmeo de Nexora.Api.Edge, como a docstring desta classe pede.
        ApiErrorCodes.OrderNotFound => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.OrderItemNotFound => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.ProductUnavailable => (StatusCodes.Status422UnprocessableEntity, true, false),

        // US-030 (Criar pedido com itens, modificadores e frações) — autoridade do dado é o edge
        // (só ele expõe OrdersController/OrderItemsController); mapeado aqui só para manter o
        // catálogo de códigos idêntico ao gêmeo de Nexora.Api.Edge.
        ApiErrorCodes.ModifierGroupRequired => (StatusCodes.Status422UnprocessableEntity, true, false),
        ApiErrorCodes.ModifierGroupSelectionInvalid => (StatusCodes.Status422UnprocessableEntity, true, false),
        ApiErrorCodes.OrderNotAcceptingItems => (StatusCodes.Status422UnprocessableEntity, true, false),
        ApiErrorCodes.OrderItemVariantPriceNotFound => (StatusCodes.Status422UnprocessableEntity, true, false),

        // Dividir a conta (US-027) — autoridade do dado é o edge (só ele expõe os controllers);
        // mapeado aqui só para manter o catálogo idêntico ao gêmeo de Nexora.Api.Edge.
        ApiErrorCodes.BillItemNotAssigned => (StatusCodes.Status422UnprocessableEntity, true, false),
        ApiErrorCodes.BillItemAssignmentInvalid => (StatusCodes.Status422UnprocessableEntity, true, false),
        ApiErrorCodes.BillInvalidAmount => (StatusCodes.Status422UnprocessableEntity, true, false),
        ApiErrorCodes.BillNotRequested => (StatusCodes.Status422UnprocessableEntity, true, false),

        // Bloquear fechamento com item pendente (US-035) — autoridade do dado é o edge (só ele
        // expõe os controllers); mapeado aqui só para manter o catálogo idêntico ao gêmeo de
        // Nexora.Api.Edge, como a docstring desta classe pede.
        ApiErrorCodes.PendingItems => (StatusCodes.Status422UnprocessableEntity, true, true),

        // Catálogo — categorias/produtos/variantes/preço por canal (US-010/US-011). Gap
        // pré-existente fechado nesta tarefa: os módulos de catálogo já declaravam esses códigos
        // em Shared/Errors/ApiErrorCodes.Catalog.cs havia tempo, mas nenhum agente anterior tinha
        // atualizado este switch ainda (cada um evitou "editar em paralelo", ver docstring desta
        // classe) — Nexora.ApiTests.Catalog.CatalogResultMappingTests já cobria exatamente estes
        // casos e falhava (404/422/503 caindo no catch-all 500) até esta correção.
        ApiErrorCodes.CategoryNotFound => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.ProductNotFound => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.ProductCategoryNotFound => (StatusCodes.Status422UnprocessableEntity, true, false),
        ApiErrorCodes.ProductStationNotFound => (StatusCodes.Status422UnprocessableEntity, true, false),
        ApiErrorCodes.CatalogReorderSetMismatch => (StatusCodes.Status422UnprocessableEntity, true, false),
        ApiErrorCodes.ProductMediaStorageUnavailable => (StatusCodes.Status503ServiceUnavailable, true, false),
        ApiErrorCodes.ProductMediaNotFound => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.PublicMenuTenantNotFound => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.VariantNotFound => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.PriceChannelInvalid => (StatusCodes.Status400BadRequest, true, false),

        // Grupos de modificadores e modificadores (US-012) — mesmo gap, fechado junto.
        ApiErrorCodes.ModifierGroupNotFound => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.ModifierNotFound => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.ModifierGroupProductNotFound => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.ModifierIngredientNotFound => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.ProductModifierGroupAlreadyLinked => (StatusCodes.Status409Conflict, true, false),
        ApiErrorCodes.ProductModifierGroupNotLinked => (StatusCodes.Status404NotFound, false, false),

        // Preço por canal de venda em tabela (US-014) — mesmo gap, fechado junto.
        ApiErrorCodes.PriceTableVariantNotFound => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.PriceTableCategoryNotFound => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.PriceTableChannelInvalid => (StatusCodes.Status400BadRequest, true, false),
        ApiErrorCodes.PriceTableChannelDuplicated => (StatusCodes.Status400BadRequest, true, false),
        ApiErrorCodes.PriceBulkAdjustNegativeResult => (StatusCodes.Status422UnprocessableEntity, true, false),

        // Tempo de preparo e praça por produto (US-016) — mesmo gap, fechado junto.
        ApiErrorCodes.PrepTimeVariantNotFound => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.PrepTimeProductNotFound => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.PrepTimeStationNotFound => (StatusCodes.Status404NotFound, false, false),

        // Praças de produção (US-017) — mesmo gap, fechado junto.
        ApiErrorCodes.StationNotFound => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.StationCodeAlreadyExists => (StatusCodes.Status409Conflict, true, false),
        ApiErrorCodes.StationHasLinkedProducts => (StatusCodes.Status422UnprocessableEntity, true, false),
        ApiErrorCodes.StationStoreContextMissing => (StatusCodes.Status403Forbidden, false, false),

        // Legados do cloud (placeholders de módulos ainda não portados — mantidos do arquivo
        // anterior a esta tarefa; nenhum handler emite hoje, mas preservam o comportamento
        // pré-existente caso algo dependa deles em Swagger/testes).
        "AFFILIATE_LOOKUP_FAILED" => (StatusCodes.Status502BadGateway, true, false),
        "EXPIRED_RESET_TOKEN" => (StatusCodes.Status410Gone, false, false),

        // Catch-all: código não catalogado -> 500, tratado como bug de mapeamento (não vaza
        // stack trace nem mensagem interna — ADR-021).
        _ => (StatusCodes.Status500InternalServerError, false, false),
    };
}
