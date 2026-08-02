namespace Awaken.Application.Common.Interfaces;

public interface IUserDateService
{
    DateOnly TodayLocal { get; }

    /// <summary>US-088: data e hora local do usuário, para calcular fração do dia transcorrida.</summary>
    DateTime NowLocal { get; }
}
