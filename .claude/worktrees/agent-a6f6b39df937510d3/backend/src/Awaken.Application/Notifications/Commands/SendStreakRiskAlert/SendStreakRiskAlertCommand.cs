using MediatR;

namespace Awaken.Application.Notifications.Commands.SendStreakRiskAlert;

public record SendStreakRiskAlertCommand() : IRequest<SendStreakRiskAlertResult>;

public record SendStreakRiskAlertResult(int Eligible, int Sent, int Skipped);
