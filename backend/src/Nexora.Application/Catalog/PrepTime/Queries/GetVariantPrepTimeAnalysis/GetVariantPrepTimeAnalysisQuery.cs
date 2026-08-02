using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Catalog;

namespace Nexora.Application.Catalog.PrepTime.Queries.GetVariantPrepTimeAnalysis;

/// <summary>US-016 — porta de <c>GET /v1/catalog/variants/{id}/prep-time-analysis</c>.</summary>
public sealed record GetVariantPrepTimeAnalysisQuery(Guid VariantId) : IQuery<PrepTimeAnalysisResponse>;
