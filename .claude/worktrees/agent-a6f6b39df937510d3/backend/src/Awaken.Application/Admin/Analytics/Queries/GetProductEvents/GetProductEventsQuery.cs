using Awaken.Contracts.Admin.Analytics;
using MediatR;

namespace Awaken.Application.Admin.Analytics.Queries.GetProductEvents;

/// <summary>US-168 — eventos do produto por volume/distribuição.</summary>
public record GetProductEventsQuery(DateTime? From, DateTime? To, string? EventNameFilter)
    : IRequest<ProductEventsResponse>;
