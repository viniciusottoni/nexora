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
