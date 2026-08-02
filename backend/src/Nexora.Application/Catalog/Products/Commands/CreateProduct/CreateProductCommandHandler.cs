using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Contracts.Catalog;
using Nexora.Domain.Catalog;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Catalog.Products.Commands.CreateProduct;

internal sealed class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<ProductResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public CreateProductCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<ProductResponse>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null || _tenantContext.UserId is null)
        {
            return Result<ProductResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado ao seu usuário.",
                ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;
        var actorId = _tenantContext.UserId.Value;
        var now = DateTimeOffset.UtcNow;

        var category = await _db.Categories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId && c.TenantId == tenantId && c.DeletedAt == null, cancellationToken);

        if (category is null)
        {
            return Result<ProductResponse>.Failure("Categoria não encontrada.", ApiErrorCodes.ProductCategoryNotFound);
        }

        string? stationName = null;
        if (request.StationId is { } stationId)
        {
            var station = await _db.Stations
                .FirstOrDefaultAsync(s => s.Id == stationId && s.TenantId == tenantId && s.DeletedAt == null, cancellationToken);

            if (station is null)
            {
                return Result<ProductResponse>.Failure("Praça não encontrada.", ApiErrorCodes.ProductStationNotFound);
            }

            stationName = station.Name;
        }

        var product = Product.Create(
            tenantId,
            category.Id,
            request.Name,
            request.StationId,
            request.Description,
            request.Allergens,
            request.AllowsFractions,
            request.MaxFractions);

        // IngredientsText e a posição inicial não fazem parte de Product.Create (agregado
        // original não previa esses dois campos no construtor estático) — reaproveita
        // UpdateDetails para atribuí-los no mesmo objeto recém-criado, sem reabrir a assinatura.
        product.UpdateDetails(
            product.Name,
            product.CategoryId,
            product.StationId,
            product.Description,
            request.IngredientsText,
            product.Allergens,
            product.AllowsFractions,
            product.MaxFractions,
            request.Position);

        if (!request.IsActive)
        {
            product.Deactivate();
        }

        _db.Products.Add(product);

        // US-011 §3.1 "Regra de que produto sem variação recebe uma variação única implícita" —
        // todo produto sempre tem ao menos uma ProductVariant (é ela, não o Product, a unidade real
        // de venda/preço). Criada sem preço aqui de propósito: CreateProductCommand não carrega
        // valor monetário nenhum (US-010 nunca modelou preço no Product) e não é reaberto agora
        // para não recontratar um contrato já mesclado/testado — o preço desta variante implícita é
        // definido logo em seguida pelo mesmo fluxo de tela (POST .../variants/{id}/prices), o que
        // do ponto de vista do gestor continua sendo "cadastrar o produto com preço único" em uma
        // única tela (US-011 §10). Não emite domain event próprio: fica coberto pelo mesmo
        // product.created desta transação (ver relatório da tarefa).
        var defaultVariant = ProductVariant.Create(tenantId, product.Id, product.Name, isDefault: true);
        _db.ProductVariants.Add(defaultVariant);

        _db.AuditLogs.Add(AuditLog.Create(
            tenantId,
            action: "PRODUCT_CREATED",
            entity: "product",
            occurredAt: now,
            actorId: actorId,
            deviceId: _tenantContext.DeviceId,
            entityId: product.Id,
            after: JsonSerializer.Serialize(new
            {
                name = product.Name,
                categoryId = product.CategoryId,
                stationId = product.StationId,
                position = product.SortOrder,
                isActive = product.IsActive
            })));

        // EVT-050 product.created (US-010 §6).
        _db.DomainEvents.Add(DomainEvent.Create(
            tenantId,
            type: "product.created",
            aggregateType: "product",
            aggregateId: product.Id,
            payload: JsonSerializer.Serialize(new { productId = product.Id, changedKeys = new[] { "created" } }),
            origin: "CLOUD",
            occurredAt: now,
            actorId: actorId,
            deviceId: _tenantContext.DeviceId));

        // SaveChangesAsync é feito pelo TransactionBehavior.

        return Result<ProductResponse>.Success(new ProductResponse(
            product.Id,
            product.CategoryId,
            category.Name,
            product.StationId,
            stationName,
            product.Name,
            product.Description,
            product.IngredientsText,
            product.Allergens,
            ImageUrl: null,
            product.SortOrder,
            product.IsActive,
            product.IsAvailable,
            product.AllowsFractions,
            product.MaxFractions));
    }
}
