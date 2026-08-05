using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Platform;

namespace Nexora.Application.Onboarding.Queries.GetOnboardingStatus;

/// <summary>Porta de <c>GET /v1/platform/tenants/{id}/onboarding</c> (US-141 §7).</summary>
public sealed record GetOnboardingStatusQuery(Guid TenantId) : IQuery<OnboardingStatusResponse>;
