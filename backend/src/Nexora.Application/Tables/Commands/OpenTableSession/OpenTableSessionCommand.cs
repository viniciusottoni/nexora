using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Operation;

namespace Nexora.Application.Tables.Commands.OpenTableSession;

/// <summary>
/// Abertura de mesa pelo garçom (US-022). Porta de <c>POST /v1/tables/{id}/sessions</c>.
/// <paramref name="OccurredAt"/> é o cabeçalho <c>X-Occurred-At</c> (RN-020, US-022 §9) — nulo usa
/// o relógio do servidor (o cliente sempre deveria mandar, mas o servidor não recusa a
/// requisição por falta dele: preferir abrir a mesa com um carimbo aproximado a recusar a operação
/// crítica de tempo real).
/// </summary>
public sealed record OpenTableSessionCommand(Guid TableId, short GuestCount, DateTimeOffset? OccurredAt)
    : ICommand<TableSessionResponse>;
