using System.Text.Json;
using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Application.Abstractions.Security;
using Nexora.Contracts.Catalog;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Catalog.PrepTime.Commands.ReassignProductStation;

/// <summary>
/// US-016 — reatribui a praça de produção de um produto (roteamento de fila do KDS é da
/// E-03/E-11, fora de escopo; aqui só garantimos que <c>stationId</c> é reatribuível). Emite
/// <c>product.updated</c> (EVT-050) na mesma transação.
/// </summary>
internal sealed class ReassignProductStationCommandHandler
    : IRequestHandler<ReassignProductStationCommand, Result<ProductStationResponse>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentTenantContext _tenantContext;

    public ReassignProductStationCommandHandler(IApplicationDbContext db, ICurrentTenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<Result<ProductStationResponse>> Handle(
        ReassignProductStationCommand request, CancellationToken cancellationToken)
    {
        if (_tenantContext.TenantId is null)
        {
            return Result<ProductStationResponse>.Failure(
                "Não foi possível identificar o estabelecimento vinculado à requisição.",
                ApiErrorCodes.TenantContextMissing);
        }

        var tenantId = _tenantContext.TenantId.Value;

        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Id == request.ProductId && p.TenantId == tenantId, cancellationToken);

        if (product is null)
        {
            return Result<ProductStationResponse>.Failure("Produto não encontrado.", ApiErrorCodes.PrepTimeProductNotFound);
        }

        Station? station = null;
        if (request.StationId is not null)
        {
            station = await _db.Stations.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == request.StationId && s.TenantId == tenantId, cancellationToken);

            if (station is null)
            {
                return Result<ProductStationResponse>.Failure("Praça não encontrada.", ApiErrorCodes.PrepTimeStationNotFound);
            }
        }

        product.ReassignStation(request.StationId);

        _db.DomainEvents.Add(DomainEvent.Create(
            tenantId,
            type: "product.updated",
            aggregateType: "product",
            aggregateId: product.Id,
            payload: JsonSerializer.Serialize(new
            {
                productId = product.Id,
                stationId = product.StationId,
            }),
            origin: "CLOUD",
            occurredAt: DateTimeOffset.UtcNow,
            actorId: _tenantContext.UserId,
            deviceId: _tenantContext.DeviceId));

        // SaveChangesAsync é feito pelo TransactionBehavior (commands).

        return Result<ProductStationResponse>.Success(new ProductStationResponse(
            product.Id, product.StationId, station?.Code, station?.Name));
    }
}
