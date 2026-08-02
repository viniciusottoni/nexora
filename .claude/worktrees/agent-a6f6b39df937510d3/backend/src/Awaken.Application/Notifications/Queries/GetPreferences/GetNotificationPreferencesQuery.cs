using Awaken.Contracts.Notifications;
using MediatR;

namespace Awaken.Application.Notifications.Queries.GetPreferences;

public record GetNotificationPreferencesQuery : IRequest<NotificationPreferencesResponse?>;
