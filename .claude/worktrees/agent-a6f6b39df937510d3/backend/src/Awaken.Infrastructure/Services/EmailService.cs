using Awaken.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Awaken.Infrastructure.Services;

public class EmailService(ILogger<EmailService> logger) : IEmailService
{
    public Task SendPasswordResetAsync(
        string toEmail,
        string rawToken,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "PASSWORD_RESET_TOKEN_GENERATED - integrate real email provider before production");
        return Task.CompletedTask;
    }
}
