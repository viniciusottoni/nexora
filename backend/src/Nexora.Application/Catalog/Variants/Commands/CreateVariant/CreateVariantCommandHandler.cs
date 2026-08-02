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

namespace Nexora.Application.Catalog.Variants.Commands.CreateVariant;

/// <summary>
/// Cria a variante e, na mesma transação, seu preço base em um único canal (US-011 §"O que
/// construir": "esta US só precisa entregar UM preço 'base'"). Se <see cref="CreateVariantCommand.IsDefault"/>
/// for <c>true</c>, desmarca qualquer outra variante padrão do mesmo produto — só pode existir uma
/// variante <c>IsDefault</c> por produto (garante a leitura "variação única implícita" do §3.1).
/// </summary>
internal sealed class CreateVariantCommandHandler : IRequestHandler<CreateVariantCommand, Result<VariantResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public CreateVariantCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<VariantResponse>> Handle(CreateVariantCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null || _tenantContext.UserId is null)
        {
            return Result<VariantResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado ao seu usuário.",
                ApiErrorCodes.TenantContextMissing);
        }

        if (!ChannelParser.TryParse(request.Channel, out var channel))
        {
            return Result<VariantResponse>.Failure("Canal de venda inválido.", ApiErrorCodes.PriceChannelInvalid);
        }

        var tenantId = _tenantContext.TenantId.Value;
        var actorId = _tenantContext.UserId.Value;
        var now = DateTimeOffset.UtcNow;

        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Id == request.ProductId && p.TenantId == tenantId && p.DeletedAt == null, cancellationToken);

        if (product is null)
        {
            return Result<VariantResponse>.Failure("Produto não encontrado.", ApiErrorCodes.ProductNotFound);
        }

        if (request.IsDefault)
        {
            var previousDefaults = await _db.ProductVariants
                .Where(v => v.ProductId == product.Id && v.TenantId == tenantId && v.DeletedAt == null && v.IsDefault)
                .ToListAsync(cancellationToken);

            foreach (var previousDefault in previousDefaults)
            {
                previousDefault.UnmarkAsDefault();
            }
        }

        var variant = ProductVariant.Create(
            tenantId,
            product.Id,
            request.Name,
            request.PrepMinutes ?? 10,
            request.IsDefault,
            request.Sku,
            request.SizeCode);

        _db.ProductVariants.Add(variant);

        var price = Price.Create(tenantId, variant.Id, channel, request.BasePrice, actorId, now);
        _db.Prices.Add(price);

        _db.AuditLogs.Add(AuditLog.Create(
            tenantId,
            action: "PRODUCT_VARIANT_CREATED",
            entity: "product_variant",
            occurredAt: now,
            actorId: actorId,
            deviceId: _tenantContext.DeviceId,
            entityId: variant.Id,
            after: JsonSerializer.Serialize(new
            {
                productId = product.Id,
                name = variant.Name,
                sizeCode = variant.SizeCode,
                isDefault = variant.IsDefault,
                channel = channel.ToString(),
                amount = price.Amount
            })));

        // EVT-050 product.updated — "Variação criada ou alterada" (US-011 §6).
        _db.DomainEvents.Add(DomainEvent.Create(
            tenantId,
            type: "product.updated",
            aggregateType: "product",
            aggregateId: product.Id,
            payload: JsonSerializer.Serialize(new { productId = product.Id, variantId = variant.Id, changedKeys = new[] { "variant_created" } }),
            origin: "CLOUD",
            occurredAt: now,
            actorId: actorId,
            deviceId: _tenantContext.DeviceId));

        // SaveChangesAsync é feito pelo TransactionBehavior.

        return Result<VariantResponse>.Success(new VariantResponse(
            variant.Id,
            variant.ProductId,
            variant.Name,
            variant.Sku,
            variant.SizeCode,
            variant.PrepMinutes,
            variant.IsDefault,
            variant.IsActive,
            price.Amount,
            channel.ToString()));
    }
}
