using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Catalog;

namespace Nexora.Application.Catalog.PrepTime.Commands.ReassignProductStation;

/// <summary>US-016 — porta de <c>PATCH /v1/catalog/products/{id}/station</c>.</summary>
public sealed record ReassignProductStationCommand(Guid ProductId, Guid? StationId) : ICommand<ProductStationResponse>;
