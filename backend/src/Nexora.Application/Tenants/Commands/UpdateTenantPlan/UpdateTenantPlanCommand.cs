using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Tenants;

namespace Nexora.Application.Tenants.Commands.UpdateTenantPlan;

/// <summary>
/// US-154 · Gestão de planos e configuração comercial — <c>PUT /v1/platform/tenants/{id}/plan</c>,
/// exclusiva da policy <c>PlatformAdmin</c>. Upgrade/downgrade com vigência: quando
/// <see cref="EffectiveAt"/> já chegou (ou é nula — vigência imediata), o plano muda na mesma
/// chamada; quando é futura, a mudança fica agendada (ver <see cref="Nexora.Domain.Platform.Tenant.SchedulePlanChange"/>).
/// </summary>
/// <param name="ExpectedVersion">Extraído do header <c>If-Match</c> pelo controller — mesmo padrão de <c>TransitionTenantStatusCommand.ExpectedVersion</c>, mas confrontado com <see cref="Nexora.Domain.Platform.Tenant.PlanVersion"/> (dimensão independente de <c>StatusVersion</c>).</param>
/// <param name="ActorId">Claim <c>sub</c> do administrador de plataforma — RN-004 "ator".</param>
public sealed record UpdateTenantPlanCommand(
    Guid TenantId,
    string Plan,
    DateTimeOffset? EffectiveAt,
    string Reason,
    int ExpectedVersion,
    Guid? ActorId) : ICommand<TenantPlanUpdateResponse>;
