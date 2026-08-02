namespace Awaken.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    bool TryGetUserId(out Guid userId);
}
