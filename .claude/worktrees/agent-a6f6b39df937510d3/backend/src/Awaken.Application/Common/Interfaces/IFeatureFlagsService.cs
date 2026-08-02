namespace Awaken.Application.Common.Interfaces;

public interface IFeatureFlagsService
{
    bool IsPremiumCardEnabled { get; }
}
