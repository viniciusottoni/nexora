namespace Awaken.Application.Common.Interfaces;

public interface IPushNotificationService
{
    Task SendAsync(string pushToken, string title, string body, Dictionary<string, string>? data = null, CancellationToken ct = default);
}
