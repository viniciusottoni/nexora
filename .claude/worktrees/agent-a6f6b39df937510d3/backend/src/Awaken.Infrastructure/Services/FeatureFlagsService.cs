using Awaken.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Awaken.Infrastructure.Services;

public class FeatureFlagsService(IConfiguration config) : IFeatureFlagsService
{
    public bool IsPremiumCardEnabled =>
        bool.TryParse(config["Features:PremiumCardEnabled"], out var enabled) ? enabled : true;
}
