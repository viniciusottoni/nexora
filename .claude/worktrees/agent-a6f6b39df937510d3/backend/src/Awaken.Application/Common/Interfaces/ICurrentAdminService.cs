namespace Awaken.Application.Common.Interfaces;

public interface ICurrentAdminService
{
    Guid AdminUserId { get; }
    bool IsAuthenticated { get; }
}
