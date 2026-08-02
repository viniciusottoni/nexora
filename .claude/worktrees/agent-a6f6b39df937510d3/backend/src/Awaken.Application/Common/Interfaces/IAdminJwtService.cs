using Awaken.Domain.Entities.Admin;

namespace Awaken.Application.Common.Interfaces;

public interface IAdminJwtService
{
    string GenerateToken(AdminUser adminUser);
}
