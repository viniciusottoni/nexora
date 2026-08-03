using Nexora.Application.Abstractions.Messaging;
using Nexora.Contracts.Operation;

namespace Nexora.Application.Tables.Commands.UpdateTableSession;

/// <summary>
/// Altera a sessão aberta (US-022 §3.1: "Alteração da contagem de pessoas durante a sessão" e
/// "Atribuição e troca de garçom responsável"). Porta de <c>PATCH /v1/sessions/{id}</c>. Os dois
/// campos são independentes e opcionais — <c>null</c> significa "não alterar este campo", nunca
/// "limpar o valor" (não existe cenário de sessão sem garçom responsável depois de atribuído).
/// </summary>
public sealed record UpdateTableSessionCommand(Guid SessionId, short? GuestCount, Guid? WaiterId)
    : ICommand<TableSessionResponse>;
