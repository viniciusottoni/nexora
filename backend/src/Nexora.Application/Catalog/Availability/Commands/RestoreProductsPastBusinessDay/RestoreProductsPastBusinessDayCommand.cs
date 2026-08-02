using Nexora.Application.Abstractions.Messaging;

namespace Nexora.Application.Catalog.Availability.Commands.RestoreProductsPastBusinessDay;

/// <summary>
/// Varre os produtos indisponíveis do tenant informado e devolve à disponibilidade todos os que
/// foram marcados indisponíveis num dia operacional anterior ao atual (US-015 §3.1, cenário
/// "Retorno automático no novo dia operacional" — ver <see cref="Availability.BusinessDayPolicy"/>
/// para a regra de virada, ADR-018). Recebe <c>TenantId</c> explicitamente (em vez de usar
/// <c>ICurrentTenantContext</c>) porque quem despacha este comando é um <c>BackgroundService</c>
/// sem requisição HTTP/JWT — mesmo padrão de <c>LoginWithPasswordCommandHandler</c>/
/// <c>ProvisionTenantCommandHandler</c> (fixam o tenant via <c>IApplicationDbContext.SetTenantContextAsync</c>
/// a partir de um tenant já conhecido por outra via, não da claim JWT).
///
/// Retorna a contagem de produtos restaurados — insumo para "o gestor deve ser informado no
/// resumo diário" (US-015 §4/§11); [PENDÊNCIA] não existe ainda um módulo de resumo diário/relatório
/// que consuma essa contagem ou o evento <c>product.availability_changed(isAvailable=true)</c>
/// gerado por produto restaurado — o `BackgroundService` que despacha este comando (ver
/// <c>Nexora.Api.Edge.Workers.AvailabilityAutoRestoreWorker</c>/
/// <c>Nexora.Api.Cloud.Workers.AvailabilityAutoRestoreWorker</c>) hoje só loga a contagem.
/// </summary>
public sealed record RestoreProductsPastBusinessDayCommand(Guid TenantId) : ICommand<int>;
