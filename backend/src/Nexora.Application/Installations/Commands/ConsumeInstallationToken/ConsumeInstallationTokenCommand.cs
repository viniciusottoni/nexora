using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Installations;

namespace Nexora.Application.Installations.Commands.ConsumeInstallationToken;

/// <summary>
/// Porta de <c>ConsumeInstallationTokenService</c> (módulo de Tenants no original — elo entre
/// o provisionamento do tenant e o pareamento físico do edge). Reserva o token de uso único
/// (marca <c>TokenConsumedAt</c>) e devolve a identidade de tenant/loja/instalação — a chave
/// pública e o restante do registro completo só chegam depois, em
/// <c>RegisterInstallationCommand</c>, quando o dispositivo físico sobe de fato.
/// </summary>
public sealed record ConsumeInstallationTokenCommand(string RawToken) : ICommand<ConsumeInstallationTokenResponse>;
