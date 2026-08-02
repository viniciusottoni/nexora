using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Catalog;

namespace Nexora.Application.Catalog.PrepTime.Commands.UpdateVariantPrepTimeThresholds;

/// <summary>US-016 — porta de <c>PATCH /v1/catalog/variants/{id}/prep-time</c>.</summary>
public sealed record UpdateVariantPrepTimeThresholdsCommand(
    Guid VariantId,
    short PrepMinutes,
    short? WarnMinutes,
    short? CriticalMinutes) : ICommand<VariantPrepTimeResponse>;
