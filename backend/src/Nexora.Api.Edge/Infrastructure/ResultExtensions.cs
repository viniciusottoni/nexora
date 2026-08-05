using System.Diagnostics;
using System.Globalization;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Shared.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Nexora.Api.Edge.Infrastructure;

/// <summary>
/// Traduz <see cref="Result"/>/<see cref="Result{T}"/> em resposta HTTP RFC 7807 (ADR-021), usando
/// o <see cref="ProblemDetails"/>/<see cref="ValidationProblemDetails"/> nativos do ASP.NET Core —
/// não mais o record próprio <c>Nexora.Contracts.Errors.ErrorResponse</c> (esse continua existindo
/// só para anotação de Swagger nos controllers; o corpo real da resposta é o que este arquivo
/// monta). Espelha o ResultExtensions do seminarioteologico. NOTA: este arquivo é reescrito/
/// estendido centralmente conforme novos códigos de erro são adicionados pelos módulos — não
/// editar em paralelo por múltiplos agentes; adicione o código em
/// Shared/Errors/ApiErrorCodes.&lt;Modulo&gt;.cs e sinalize para <see cref="MapErrorCode"/> ser
/// atualizado depois.
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
    /// Extrai convenções históricas de "chave reservada dentro de Errors" (usadas antes deste
    /// contrato ter um campo <c>meta</c> de verdade) para <c>Extensions["meta"][...]</c>: (1)
    /// <c>retryAfterSeconds</c> — LoginWithPin/LoginWithPassword/AuthorizeSensitiveAction, formato
    /// que o frontend já lê (packages/ui/src/auth/operational-auth-client.ts,
    /// <c>problem.meta?.retryAfterSeconds</c>); (2) <c>sessionId</c> — US-022, cenário "Mesa já
    /// ocupada": 409 <see cref="ApiErrorCodes.TableAlreadyOpen"/> direciona o chamador à sessão
    /// existente (US-022 §7: <c>meta: { sessionId }</c>). Não exige alterar a assinatura de
    /// <see cref="Result{T}.Failure"/>: os handlers usam o dicionário de <c>errors</c> já existente
    /// como transporte, e a conversão para <c>meta</c> acontece só aqui, no mapeamento genérico.
    /// </summary>
    private static Dictionary<string, object>? ExtractMeta(ref IReadOnlyDictionary<string, string[]>? fieldErrors)
    {
        if (fieldErrors is null || fieldErrors.Count == 0)
        {
            return null;
        }

        Dictionary<string, object>? meta = null;

        if (fieldErrors.TryGetValue("retryAfterSeconds", out var retryValues) && retryValues.Length > 0 &&
            int.TryParse(retryValues[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds))
        {
            (meta ??= new Dictionary<string, object>())["retryAfterSeconds"] = seconds;
        }

        if (fieldErrors.TryGetValue("sessionId", out var sessionIdValues) && sessionIdValues.Length > 0)
        {
            (meta ??= new Dictionary<string, object>())["sessionId"] = sessionIdValues[0];
        }

        // US-030 §7: 422 MODIFIER_GROUP_REQUIRED/MODIFIER_GROUP_SELECTION_INVALID { itemIndex,
        // groupId, groupName } e 422 PRODUCT_UNAVAILABLE { variantId } — mesma convenção acima
        // (chave reservada em Errors, convertida para meta só aqui).
        if (fieldErrors.TryGetValue("itemIndex", out var itemIndexValues) && itemIndexValues.Length > 0 &&
            int.TryParse(itemIndexValues[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var itemIndex))
        {
            (meta ??= new Dictionary<string, object>())["itemIndex"] = itemIndex;
        }

        if (fieldErrors.TryGetValue("groupId", out var groupIdValues) && groupIdValues.Length > 0)
        {
            (meta ??= new Dictionary<string, object>())["groupId"] = groupIdValues[0];
        }

        if (fieldErrors.TryGetValue("groupName", out var groupNameValues) && groupNameValues.Length > 0)
        {
            (meta ??= new Dictionary<string, object>())["groupName"] = groupNameValues[0];
        }

        if (fieldErrors.TryGetValue("variantId", out var variantIdValues) && variantIdValues.Length > 0)
        {
            (meta ??= new Dictionary<string, object>())["variantId"] = variantIdValues[0];
        }

        // US-035 §7: 422 PENDING_ITEMS { meta: { pendingItems: [{ name, status }] } } — valor é o
        // JSON já serializado da lista inteira (não um escalar simples como os casos acima), então
        // é reidratado como JsonElement para sair como array de objetos na resposta, não como string.
        if (fieldErrors.TryGetValue(Nexora.Application.Tables.Support.PendingItemsClosePolicy.MetaErrorsKey, out var pendingItemsValues) &&
            pendingItemsValues.Length > 0)
        {
            (meta ??= new Dictionary<string, object>())["pendingItems"] =
                System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(pendingItemsValues[0]);
        }

        // US-033 §7: 403 AUTHORIZATION_REQUIRED { meta: { action, itemStatus } } — cancelamento de
        // item/pedido já iniciado sem token válido. Mesma convenção de chave reservada acima.
        if (fieldErrors.TryGetValue("action", out var actionValues) && actionValues.Length > 0)
        {
            (meta ??= new Dictionary<string, object>())["action"] = actionValues[0];
        }

        if (fieldErrors.TryGetValue("itemStatus", out var itemStatusValues) && itemStatusValues.Length > 0)
        {
            (meta ??= new Dictionary<string, object>())["itemStatus"] = itemStatusValues[0];
        }

        if (meta is null)
        {
            return null;
        }

        var reservedKeys = new HashSet<string>(StringComparer.Ordinal)
        {
            "retryAfterSeconds", "sessionId", "itemIndex", "groupId", "groupName", "variantId", "action", "itemStatus",
            Nexora.Application.Tables.Support.PendingItemsClosePolicy.MetaErrorsKey,
        };
        var remaining = fieldErrors.Where(kv => !reservedKeys.Contains(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value);
        fieldErrors = remaining.Count == 0 ? null : remaining;

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

        // Backup (edge) — ver módulo Backups.
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

        // Ciclo de vida do estabelecimento (US-153) — autoridade do dado é o cloud (só ele expõe
        // TenantsController); mapeado aqui só para manter o catálogo idêntico ao gêmeo de
        // Nexora.Api.Cloud, como a docstring desta classe pede.
        ApiErrorCodes.TenantStatusTransitionInvalid => (StatusCodes.Status409Conflict, true, false),
        ApiErrorCodes.ConcurrencyConflict => (StatusCodes.Status409Conflict, true, false),
        ApiErrorCodes.ReasonRequired => (StatusCodes.Status422UnprocessableEntity, true, false),

        // Operação — ambientes e mesas do salão (US-020). Nenhum controller de escrita existe
        // hoje em Nexora.Api.Edge (autoridade do dado é a nuvem, US-020 cabeçalho "Aplicações:
        // web-admin, api-cloud") — mapeado aqui só para manter o catálogo de códigos idêntico ao
        // gêmeo de Nexora.Api.Cloud, como a docstring desta classe pede.
        ApiErrorCodes.AreaNotFound => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.AreaHasActiveTables => (StatusCodes.Status422UnprocessableEntity, true, false),
        ApiErrorCodes.TableNotFound => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.TableLabelAlreadyExists => (StatusCodes.Status409Conflict, true, false),
        ApiErrorCodes.TableHasSessionHistory => (StatusCodes.Status422UnprocessableEntity, true, false),
        ApiErrorCodes.TablesExportEmpty => (StatusCodes.Status422UnprocessableEntity, true, false),

        // Sessão de mesa (US-021/US-022) — autoridade do dado é local (RF-SAL-04/RF-SAL-02):
        // POST/PATCH/GET de sessão e a resolução pública de qr_token vivem só aqui, no edge.
        ApiErrorCodes.TableAlreadyOpen => (StatusCodes.Status409Conflict, true, false),
        ApiErrorCodes.InvalidTableToken => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.TableSessionNotFound => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.TableSessionNotOpen => (StatusCodes.Status422UnprocessableEntity, true, false),

        // Chamar garçom (US-025) e solicitar a conta (US-026) — alerta dirigido via Nexora.Domain.Metrics.Alert.
        ApiErrorCodes.NoPendingWaiterCall => (StatusCodes.Status422UnprocessableEntity, true, false),

        // Dividir a conta (US-027) — divisão por item, retirada de taxa e pagamento parcial.
        ApiErrorCodes.BillItemNotAssigned => (StatusCodes.Status422UnprocessableEntity, true, false),
        ApiErrorCodes.BillItemAssignmentInvalid => (StatusCodes.Status422UnprocessableEntity, true, false),
        ApiErrorCodes.BillInvalidAmount => (StatusCodes.Status422UnprocessableEntity, true, false),
        ApiErrorCodes.BillNotRequested => (StatusCodes.Status422UnprocessableEntity, true, false),

        // Bloquear fechamento com item pendente (US-035) — mesma semântica de
        // AuthorizationRequired (requiresAuthorization=true): o caixa pode reenviar autorizado.
        ApiErrorCodes.PendingItems => (StatusCodes.Status422UnprocessableEntity, true, true),

        // Consumo da mesa em tempo real (US-024) e repetição de item (US-028) — gap de US-030,
        // ver docstring de AddOrderItemCommandHandler.
        ApiErrorCodes.OrderNotFound => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.OrderItemNotFound => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.ProductUnavailable => (StatusCodes.Status422UnprocessableEntity, true, false),
        ApiErrorCodes.OrderItemVariantPriceNotFound => (StatusCodes.Status422UnprocessableEntity, true, false),

        // Cancelar item ou pedido com autorização (US-033) — 409, família CONFLICT_* do ADR-021
        // (não é erro de validação de entrada, é conflito de estado: pedido fechado/cancelado, ou
        // item já servido/já cancelado — a orientação de "detail" aponta o fluxo de estorno).
        ApiErrorCodes.InvalidStateTransition => (StatusCodes.Status409Conflict, true, false),

        // Criar pedido com itens, modificadores e frações (US-030).
        ApiErrorCodes.ModifierGroupRequired => (StatusCodes.Status422UnprocessableEntity, true, false),
        ApiErrorCodes.ModifierGroupSelectionInvalid => (StatusCodes.Status422UnprocessableEntity, true, false),
        ApiErrorCodes.OrderNotAcceptingItems => (StatusCodes.Status422UnprocessableEntity, true, false),

        // Catálogo (US-010/US-011/US-015) — o edge só expõe disponibilidade
        // (ProductAvailabilityController, US-015), mas MarkProductUnavailable/MarkProductAvailable
        // podem devolver "PRODUCT_NOT_FOUND" (mesmo valor de ApiErrorCodes.ProductNotFound) — gap
        // pré-existente fechado junto com o mesmo bloco em Nexora.Api.Cloud/Infrastructure/ResultExtensions.cs,
        // para manter os dois catálogos de código idênticos como esta classe já pede.
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
        ApiErrorCodes.ModifierGroupNotFound => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.ModifierNotFound => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.ModifierGroupProductNotFound => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.ModifierIngredientNotFound => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.ProductModifierGroupAlreadyLinked => (StatusCodes.Status409Conflict, true, false),
        ApiErrorCodes.ProductModifierGroupNotLinked => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.PriceTableVariantNotFound => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.PriceTableCategoryNotFound => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.PriceTableChannelInvalid => (StatusCodes.Status400BadRequest, true, false),
        ApiErrorCodes.PriceTableChannelDuplicated => (StatusCodes.Status400BadRequest, true, false),
        ApiErrorCodes.PriceBulkAdjustNegativeResult => (StatusCodes.Status422UnprocessableEntity, true, false),
        ApiErrorCodes.PrepTimeVariantNotFound => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.PrepTimeProductNotFound => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.PrepTimeStationNotFound => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.StationNotFound => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.StationCodeAlreadyExists => (StatusCodes.Status409Conflict, true, false),
        ApiErrorCodes.StationHasLinkedProducts => (StatusCodes.Status422UnprocessableEntity, true, false),
        ApiErrorCodes.StationStoreContextMissing => (StatusCodes.Status403Forbidden, false, false),

        // Motor de alertas e notificações (E-08).
        ApiErrorCodes.AlertNotFound => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.AlertAlreadyResolved => (StatusCodes.Status409Conflict, true, false),
        ApiErrorCodes.PushSubscriptionInvalid => (StatusCodes.Status400BadRequest, true, false),

        // E-14 · Plataforma em Escala — painel de instalações (US-140) reusa InstallationNotFound,
        // já mapeado acima. Os demais módulos abaixo entram nesta tarefa de integração final —
        // mantido idêntico ao gêmeo de Nexora.Api.Cloud (só os dois códigos legados do cloud não
        // se repetem aqui).
        ApiErrorCodes.SupportAccessNotFound => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.SupportAccessTokenNotFound => (StatusCodes.Status401Unauthorized, false, false),
        ApiErrorCodes.SupportAccessTokenExpired => (StatusCodes.Status401Unauthorized, false, false),
        ApiErrorCodes.SupportAccessTokenRevoked => (StatusCodes.Status401Unauthorized, false, false),

        ApiErrorCodes.OnboardingIncomplete => (StatusCodes.Status422UnprocessableEntity, true, false),
        ApiErrorCodes.OnboardingStepNotFound => (StatusCodes.Status404NotFound, false, false),

        ApiErrorCodes.CatalogImportInvalidFile => (StatusCodes.Status400BadRequest, true, false),
        ApiErrorCodes.CatalogImportValidationFailed => (StatusCodes.Status422UnprocessableEntity, true, false),

        ApiErrorCodes.TenantDomainAlreadyRegistered => (StatusCodes.Status422UnprocessableEntity, true, false),
        ApiErrorCodes.TenantDomainVerificationFailed => (StatusCodes.Status422UnprocessableEntity, true, false),
        ApiErrorCodes.TenantDomainNotFound => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.TenantDomainCertificateIssuanceFailed => (StatusCodes.Status503ServiceUnavailable, true, false),

        ApiErrorCodes.BusinessTemplateNotFound => (StatusCodes.Status404NotFound, false, false),

        ApiErrorCodes.ReleaseNotFound => (StatusCodes.Status404NotFound, false, false),
        ApiErrorCodes.ReleaseRolloutCannotDecrease => (StatusCodes.Status422UnprocessableEntity, true, false),

        // Catch-all: código não catalogado -> 500, tratado como bug de mapeamento (não vaza
        // stack trace nem mensagem interna — ADR-021).
        _ => (StatusCodes.Status500InternalServerError, false, false),
    };
}
