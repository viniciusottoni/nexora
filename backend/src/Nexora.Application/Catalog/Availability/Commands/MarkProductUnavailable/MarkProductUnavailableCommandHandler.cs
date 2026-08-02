using System.Text.Json;
using Nexora.Application.Abstractions.Events;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Realtime;
using Nexora.Application.Abstractions.Security;
using Nexora.Contracts.Catalog;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Catalog.Availability.Commands.MarkProductUnavailable;

/// <summary>
/// Cenário Gherkin "Propagação imediata" (US-015 §4): marca o produto indisponível
/// (<see cref="Domain.Catalog.Product.MarkUnavailable"/>), grava auditoria + evento de domínio
/// EVT-051 <c>product.availability_changed</c> (US-015 §6) na MESMA transação (ADR-006) e, antes
/// de retornar, propaga via <see cref="IAvailabilityBroadcaster"/> — chamada síncrona, aguardada
/// dentro deste <c>Handle</c>, nunca enfileirada (ver docstring de <see cref="IAvailabilityBroadcaster"/>).
/// Marcar de novo um produto já indisponível ATUALIZA o motivo (não é erro) — mesmo espírito
/// idempotente de <c>SetVariantPriceCommandHandler</c>, mas aqui sempre grava evento/auditoria
/// porque o motivo pode ter mudado (ex.: "acabou o insumo" → "praça fechada").
/// </summary>
internal sealed class MarkProductUnavailableCommandHandler
    : IRequestHandler<MarkProductUnavailableCommand, Result<ProductAvailabilityResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly IEventOriginProvider _eventOrigin;
    private readonly IAvailabilityBroadcaster _broadcaster;

    public MarkProductUnavailableCommandHandler(
        IApplicationDbContext db,
        ICurrentTenantContext tenantContext,
        IEventOriginProvider eventOrigin,
        IAvailabilityBroadcaster broadcaster)
    {
        _db = db;
        _tenantContext = tenantContext;
        _eventOrigin = eventOrigin;
        _broadcaster = broadcaster;
    }

    public async Task<Result<ProductAvailabilityResponse>> Handle(
        MarkProductUnavailableCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null || _tenantContext.UserId is null)
        {
            return Result<ProductAvailabilityResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado ao seu usuário.",
                ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;
        var actorId = _tenantContext.UserId.Value;
        var now = DateTimeOffset.UtcNow;

        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Id == request.ProductId && p.TenantId == tenantId && p.DeletedAt == null, cancellationToken);

        if (product is null)
        {
            // [DESVIO DOCUMENTADO] "PRODUCT_NOT_FOUND" é um literal, não Nexora.Shared.Errors.ApiErrorCodes.ProductNotFound
            // (US-010) — este worktree isolado não tem ApiErrorCodes.Catalog.cs nem o restante da
            // camada Application/Api de Products/Categories/Variants (só Domain+Infrastructure+schema
            // de catálogo foram commitados na criação do worktree; o resto existe só como trabalho
            // não commitado no checkout principal, fora do histórico git — `git worktree add` não
            // copia isso). Usar o MESMO valor de string do código estável já convencionado por
            // US-010 evita dependência de compilação num símbolo ausente aqui, e continua correto
            // depois do merge (quando ApiErrorCodes.Catalog.cs existir). Reportado no relatório final.
            return Result<ProductAvailabilityResponse>.Failure("Produto não encontrado.", "PRODUCT_NOT_FOUND");
        }

        var wasAvailable = product.IsAvailable;
        var previousReason = product.UnavailableReason;
        var previousAutoRestore = product.AutoRestoreNextDay;

        product.MarkUnavailable(request.Reason, request.AutoRestoreNextDay);

        // Só grava auditoria/evento quando algo de fato mudou (ficou indisponível agora, ou o
        // motivo mudou) — mesmo espírito condicional de UpdateStationCommandHandler/SetVariantPrice.
        if (wasAvailable || previousReason != product.UnavailableReason || previousAutoRestore != product.AutoRestoreNextDay)
        {
            _db.AuditLogs.Add(AuditLog.Create(
                tenantId,
                action: "PRODUCT_MARKED_UNAVAILABLE",
                entity: "product",
                occurredAt: now,
                storeId: _tenantContext.StoreId,
                actorId: actorId,
                deviceId: _tenantContext.DeviceId,
                entityId: product.Id,
                before: JsonSerializer.Serialize(new { isAvailable = wasAvailable, reason = previousReason, autoRestoreNextDay = previousAutoRestore }),
                after: JsonSerializer.Serialize(new { isAvailable = product.IsAvailable, reason = product.UnavailableReason, autoRestoreNextDay = product.AutoRestoreNextDay })));

            // EVT-051 product.availability_changed (US-015 §6) — evento bidirecional (↕): a
            // convergência por menor occurredAt entre marcação simultânea no KDS e no painel
            // (RN-019) depende de um módulo de sincronização de eventos que ainda não existe nesta
            // solution (ver IAvailabilityBroadcaster) — aqui só a gravação local é garantida.
            _db.DomainEvents.Add(DomainEvent.Create(
                tenantId,
                type: "product.availability_changed",
                aggregateType: "product",
                aggregateId: product.Id,
                payload: JsonSerializer.Serialize(new
                {
                    productId = product.Id,
                    isAvailable = false,
                    reason = product.UnavailableReason,
                    unavailableSince = product.UnavailableSince,
                    autoRestoreNextDay = product.AutoRestoreNextDay
                }),
                origin: _eventOrigin.Origin,
                occurredAt: now,
                storeId: _tenantContext.StoreId,
                actorId: actorId,
                deviceId: _tenantContext.DeviceId));
        }

        // SaveChangesAsync é feito pelo TransactionBehavior — mas o broadcast abaixo roda ANTES
        // dele terminar, aguardado de forma síncrona dentro deste Handle (não é enfileirado para
        // "depois"; ver docstring de IAvailabilityBroadcaster e o teste que prova isso).
        await _broadcaster.ProductMarkedUnavailableAsync(
            tenantId, product.Id, product.UnavailableReason!, product.UnavailableSince!.Value, cancellationToken);

        return Result<ProductAvailabilityResponse>.Success(new ProductAvailabilityResponse(
            product.Id, product.Name, product.IsAvailable, product.UnavailableReason, product.UnavailableSince));
    }
}
