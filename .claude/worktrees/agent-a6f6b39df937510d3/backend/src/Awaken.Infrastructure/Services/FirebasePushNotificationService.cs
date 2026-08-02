using Awaken.Application.Common.Interfaces;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Awaken.Infrastructure.Services;

/// US-092: envia push notifications via Firebase Cloud Messaging (FCM).
/// Se a credencial nao estiver configurada (dev/test), loga aviso e retorna sem enviar.
public class FirebasePushNotificationService : IPushNotificationService
{
    private readonly ILogger<FirebasePushNotificationService> _logger;
    private readonly bool _initialized;

    private static readonly Lock _initLock = new();

    public FirebasePushNotificationService(IConfiguration config, ILogger<FirebasePushNotificationService> logger)
    {
        _logger = logger;

        var credentialJson = config["Firebase:CredentialJson"];

        if (string.IsNullOrWhiteSpace(credentialJson))
        {
            _logger.LogWarning("Firebase:CredentialJson not configured — push notifications disabled.");
            _initialized = false;
            return;
        }

        lock (_initLock)
        {
            if (FirebaseApp.DefaultInstance is null)
            {
                var serviceAccountCredential =
                    CredentialFactory.FromJson<ServiceAccountCredential>(credentialJson);

                FirebaseApp.Create(new AppOptions
                {
                    Credential = serviceAccountCredential.ToGoogleCredential()
                });
            }
        }

        _initialized = true;
    }

    public async Task SendAsync(
        string pushToken,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        CancellationToken ct = default)
    {
        if (!_initialized)
        {
            _logger.LogWarning("push_skipped reason=firebase_not_initialized");
            return;
        }

        var message = new Message
        {
            Token = pushToken,
            Notification = new Notification
            {
                Title = title,
                Body = body
            },
            Data = data
        };

        await FirebaseMessaging.DefaultInstance.SendAsync(message, ct);
    }
}
