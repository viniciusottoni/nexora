using Awaken.Application.Common.Interfaces;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Awaken.Infrastructure.Services;

public class GoogleTokenValidator(IConfiguration config, IWebHostEnvironment environment) : IGoogleTokenValidator
{
    public async Task<GoogleTokenPayload?> ValidateAsync(string idToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var allowedIds = GetAllowedClientIds();

            if (allowedIds.Length == 0 && !environment.IsDevelopment())
                return null;

            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = allowedIds.Length > 0 ? allowedIds : null
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

            if (!payload.EmailVerified)
                return null;

            return new GoogleTokenPayload(
                payload.Subject,
                payload.Email,
                payload.EmailVerified,
                payload.Name,
                payload.Picture);
        }
        catch (InvalidJwtException)
        {
            return null;
        }
    }

    private string[] GetAllowedClientIds()
    {
        var single = config["Google:ClientId"];
        var multiple = config.GetSection("Google:AllowedClientIds").Get<string[]>() ?? [];

        var all = new List<string>(multiple);
        if (!string.IsNullOrWhiteSpace(single) && !all.Contains(single))
            all.Add(single);

        return all.Where(id => !string.IsNullOrWhiteSpace(id)).ToArray();
    }
}
