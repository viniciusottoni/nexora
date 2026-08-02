using Awaken.Contracts.Notifications;
using MediatR;

namespace Awaken.Application.Notifications.Commands.UpdateReminderTime;

public record UpdateReminderTimeCommand(TimeOnly PreferredReminderTime, string Timezone) : IRequest<NotificationPreferencesResponse>;
