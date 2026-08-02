using Awaken.Contracts.Subscriptions;
using MediatR;

namespace Awaken.Application.Subscriptions.Commands.SyncEntitlement;

/// <summary>
/// US-194: status sync initiated by the app.
/// The command carries the current RevenueCat customer ID and an optional
/// fallback alias (usually the original anonymous ID). The backend still
/// returns its authoritative subscription state. Plan/expiry/entitlement are
/// NEVER accepted from the client (ADR-009) — except when RevenueCat:DevModeEnabled
/// = true and server-side validation returns inactive (sandbox testing only).
/// </summary>
public record SyncEntitlementCommand(
    string RevenueCatCustomerId,
    string? OriginalRevenueCatCustomerId = null,
    string? ClientPlan = null,
    string? ClientEntitlement = null,
    DateTime? ClientExpiresAt = null) : IRequest<SyncEntitlementResponse>;
