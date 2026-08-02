using MediatR;

namespace Awaken.Application.Subscriptions.Commands.ProcessRevenueCatWebhook;

/// <summary>
/// US-194: processes an inbound RevenueCat webhook event.
/// All subscription state changes MUST flow through this command — the app
/// is never trusted to report plan/expiry directly (ADR-009).
/// </summary>
public record ProcessRevenueCatWebhookCommand(
    string EventId,
    string AppUserId,
    string EventType,
    string? ProductId,
    string? OriginalTransactionId,
    DateTime? ExpiresAtUtc,
    string PayloadHash) : IRequest<ProcessRevenueCatWebhookResult>;

public record ProcessRevenueCatWebhookResult(bool Processed, bool Skipped, string Reason);
