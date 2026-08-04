using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Alerts;

namespace Nexora.Application.Alerts.Commands.UpdateTenantThresholds;

/// <summary>
/// US-080 §7 <c>PATCH /v1/tenant/thresholds</c> — autoridade do dado é a nuvem (US-080 cabeçalho
/// "Autoridade do dado: Nuvem (configuração dos limiares)"); o edge só lê (config desce pelo pull,
/// US-063). Todo campo é opcional — PATCH parcial, mescla sobre o valor atual (US-080 §4, cenário
/// "Limiar configurável por tenant").
/// </summary>
public sealed record UpdateTenantThresholdsCommand(UpdateTenantThresholdsRequest Request) : ICommand<TenantThresholdsResponse>;
