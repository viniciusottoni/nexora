using MediatR;

namespace Nexora.Application.Abstractions.Messaging;

/// <summary>Comando sem retorno de dado (só sucesso/falha) — passa por TransactionBehavior.</summary>
public interface ICommand : IRequest<Result>
{
}

/// <summary>Comando com retorno de dado — passa por TransactionBehavior.</summary>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>
{
}

/// <summary>
/// Marker for the rare command that deliberately persists security state before returning a
/// business failure (for example, a brute-force attempt counter). The handler must perform its
/// own <c>SaveChangesAsync</c>; the transaction behavior only commits that deliberate write.
/// </summary>
public interface IPersistsStateOnFailureCommand
{
}
