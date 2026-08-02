using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Contracts.Catalog;
using Nexora.Domain.Common;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Catalog.PrepTime.Commands.UpdateVariantPrepTimeThresholds;

/// <summary>
/// US-016 — grava tempo de preparo e limiares de atenção/crítico de uma variação e emite
/// <c>product.updated</c> (EVT-050, ADR-006) na mesma transação (<c>TransactionBehavior</c>).
/// </summary>
/// <remarks>
/// [PENDÊNCIA] <c>ProductVariant.WarnMinutes</c>/<c>CriticalMinutes</c> ainda não têm coluna
/// física em <c>product_variant</c> nem mapeamento em <c>ProductVariantConfiguration</c> — ver
/// docstring de <c>ProductVariant.UpdatePrepTimeThresholds</c>. Este handler já está pronto para
/// quando a migration existir; até lá, <c>SaveChangesAsync</c> falhará ao tentar persistir os
/// dois campos novos se o valor informado for diferente de nulo.
/// </remarks>
internal sealed class UpdateVariantPrepTimeThresholdsCommandHandler
    : IRequestHandler<UpdateVariantPrepTimeThresholdsCommand, Result<VariantPrepTimeResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public UpdateVariantPrepTimeThresholdsCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<VariantPrepTimeResponse>> Handle(
        UpdateVariantPrepTimeThresholdsCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result<VariantPrepTimeResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado à requisição.",
                ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;

        // Tracked (não AsNoTracking): o handler muda o agregado e o TransactionBehavior salva.
        var variant = await _db.ProductVariants
            .FirstOrDefaultAsync(v => v.Id == request.VariantId && v.TenantId == tenantId, cancellationToken);

        if (variant is null)
        {
            // 404 mesmo se a variação existir em outro tenant — nunca 403 (ADR-021).
            return Result<VariantPrepTimeResponse>.Failure("Variação não encontrada.", ApiErrorCodes.PrepTimeVariantNotFound);
        }

        try
        {
            variant.UpdatePrepTimeThresholds(request.PrepMinutes, request.WarnMinutes, request.CriticalMinutes);
        }
        catch (DomainException ex)
        {
            // Segunda linha de defesa — o validator já cobre a mesma invariante (ver
            // UpdateVariantPrepTimeThresholdsCommandValidator), mas o Domain é quem tem a
            // palavra final sobre sua própria regra.
            return Result<VariantPrepTimeResponse>.Failure(ex.Message, ApiErrorCodes.ValidationError);
        }

        var occurredAt = DateTimeOffset.UtcNow;

        // EVT-050 product.updated (documento da US-016, §6) — payload mínimo do contrato de API,
        // "variantId, prepMinutes, stationId" — stationId pertence ao Product, não à variação;
        // omitido aqui de propósito (ver ReassignProductStationCommandHandler, que emite o mesmo
        // tipo de evento para a mudança de praça).
        _db.DomainEvents.Add(DomainEvent.Create(
            tenantId,
            type: "product.updated",
            aggregateType: "product_variant",
            aggregateId: variant.Id,
            payload: JsonSerializer.Serialize(new
            {
                variantId = variant.Id,
                prepMinutes = variant.PrepMinutes,
                warnMinutes = variant.WarnMinutes,
                criticalMinutes = variant.CriticalMinutes,
            }),
            origin: "CLOUD", // autoridade do dado é a nuvem (cabeçalho da US-016) — mesmo padrão de UpdateBrandingCommandHandler.
            occurredAt: occurredAt,
            actorId: _tenantContext.UserId,
            deviceId: _tenantContext.DeviceId));

        // SaveChangesAsync é feito pelo TransactionBehavior (commands).

        return Result<VariantPrepTimeResponse>.Success(new VariantPrepTimeResponse(
            variant.Id, variant.PrepMinutes, variant.WarnMinutes, variant.CriticalMinutes));
    }
}
