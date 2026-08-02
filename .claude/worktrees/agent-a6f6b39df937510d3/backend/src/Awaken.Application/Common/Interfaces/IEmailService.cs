namespace Awaken.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendPasswordResetAsync(
        string toEmail,
        string rawToken,
        CancellationToken cancellationToken = default);
}
