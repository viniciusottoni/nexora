using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Application.Catalog.Prices.Queries.ListVariantPricesByChannel;
using Nexora.Contracts.Catalog;
using Nexora.Domain.Catalog;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Catalog.Prices.Commands.SetVariantChannelPrice;

/// <summary>
/// Duplica deliberadamente a lógica de "fechar o vigente e criar um novo" já usada por
/// <c>SetVariantPriceCommandHandler</c> (US-011) em vez de reaproveitá-la — histórias paralelas
/// (US-011/US-012/US-015/US-016) estão em desenvolvimento simultâneo em worktrees isolados;
/// chamar um command de outro módulo criaria acoplamento entre trabalhos que ainda vão ser
/// mesclados manualmente. A regra em si (<see cref="Price.Close"/> + <see cref="Price.Create"/>,
/// nunca editar uma linha existente) é pequena o bastante para duplicar sem custo de manutenção
/// relevante.
/// </summary>
internal sealed class SetVariantChannelPriceCommandHandler : IRequestHandler<SetVariantChannelPriceCommand, Result<VariantPriceTableResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public SetVariantChannelPriceCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<VariantPriceTableResponse>> Handle(SetVariantChannelPriceCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null || _tenantContext.UserId is null)
        {
            return Result<VariantPriceTableResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado ao seu usuário.",
                ApiErrorCodes.TenantContextMissing);
        }

        var parsed = new List<(Channel Channel, decimal Amount)>();
        foreach (var entry in request.Prices)
        {
            if (!PricingChannelParser.TryParse(entry.Channel, out var channel))
            {
                return Result<VariantPriceTableResponse>.Failure("Canal de venda inválido.", ApiErrorCodes.PriceTableChannelInvalid);
            }

            parsed.Add((channel, entry.Amount));
        }

        if (parsed.Select(p => p.Channel).Distinct().Count() != parsed.Count)
        {
            return Result<VariantPriceTableResponse>.Failure(
                "Cada canal só pode ser definido uma vez por chamada.",
                ApiErrorCodes.PriceTableChannelDuplicated);
        }

        var tenantId = _tenantContext.TenantId.Value;
        var actorId = _tenantContext.UserId.Value;
        var now = DateTimeOffset.UtcNow;

        var variant = await _db.ProductVariants
            .FirstOrDefaultAsync(v => v.Id == request.VariantId && v.TenantId == tenantId && v.DeletedAt == null, cancellationToken);

        if (variant is null)
        {
            return Result<VariantPriceTableResponse>.Failure("Variante não encontrada.", ApiErrorCodes.PriceTableVariantNotFound);
        }

        var currentPrices = await _db.Prices
            .AsNoTracking()
            .Where(p => p.VariantId == variant.Id && p.ValidTo == null)
            .ToListAsync(cancellationToken);

        // Visão em memória do conjunto de preços vigentes após as mudanças desta chamada — usada
        // só para montar a resposta (tabela completa por canal, com herança já resolvida) sem
        // precisar reconsultar o banco antes do SaveChangesAsync (feito depois, pelo
        // TransactionBehavior — ADR-006, uma única transação para todas as mudanças).
        var effective = currentPrices.ToDictionary(p => p.Channel, p => p);

        foreach (var (channel, amount) in parsed)
        {
            effective.TryGetValue(channel, out var current);

            if (current is not null && current.Amount == amount)
            {
                // Mesmo valor do preço vigente do canal — no-op, não historiza nem emite evento
                // (mesmo espírito de SetVariantPriceCommandHandler, US-011).
                continue;
            }

            var oldAmount = current?.Amount;
            if (current is not null)
            {
                await _db.Prices
                    .Where(p => p.Id == current.Id && p.ValidTo == null)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.ValidTo, now), cancellationToken);
            }

            var newPrice = Price.Create(tenantId, variant.Id, channel, amount, actorId, now);
            _db.Prices.Add(newPrice);
            effective[channel] = newPrice;

            _db.AuditLogs.Add(AuditLog.Create(
                tenantId,
                action: "PRICE_CHANGED",
                entity: "price",
                occurredAt: now,
                actorId: actorId,
                deviceId: _tenantContext.DeviceId,
                entityId: newPrice.Id,
                before: oldAmount is null ? null : JsonSerializer.Serialize(new { amount = oldAmount, channel = channel.ToString() }),
                after: JsonSerializer.Serialize(new { amount = newPrice.Amount, channel = channel.ToString() })));

            // EVT-052 price.changed (US-014 §6) — payload inclui "channel" (uma chamada pode
            // alterar mais de um canal, diferente do POST de canal único da US-011).
            _db.DomainEvents.Add(DomainEvent.Create(
                tenantId,
                type: "price.changed",
                aggregateType: "product_variant",
                aggregateId: variant.Id,
                payload: JsonSerializer.Serialize(new
                {
                    variantId = variant.Id,
                    channel = channel.ToString(),
                    oldAmount,
                    newAmount = newPrice.Amount,
                    validFrom = newPrice.ValidFrom
                }),
                origin: "CLOUD",
                occurredAt: now,
                actorId: actorId,
                deviceId: _tenantContext.DeviceId));
        }

        // SaveChangesAsync é feito pelo TransactionBehavior.

        var rows = ChannelPriceResolver.ResolveAll(effective.Values.ToList())
            .Select(resolved => new VariantChannelPriceRow(resolved.Channel.ToString(), resolved.Amount, resolved.IsInherited, resolved.ValidFrom))
            .ToList();

        return Result<VariantPriceTableResponse>.Success(new VariantPriceTableResponse(variant.Id, variant.ProductId, rows));
    }
}
