using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Admin.Auth;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Awaken.Application.Admin.Auth.Commands.AdminLogin;

public class AdminLoginCommandHandler(
    IAdminUserRepository adminUserRepository,
    IPasswordHasher passwordHasher,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    IAuditLogService auditLogService,
    ILogger<AdminLoginCommandHandler> logger) : IRequestHandler<AdminLoginCommand, AdminLoginResponse>
{
    public async Task<AdminLoginResponse> Handle(AdminLoginCommand request, CancellationToken cancellationToken)
    {
        var utcNow = dateTimeService.UtcNow;
        var adminUser = await adminUserRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (adminUser is null)
        {
            await auditLogService.RecordAsync(
                AuditActions.AdminAuthLoginFailed,
                null,
                AuditActorType.Admin,
                AuditResourceTypes.AdminUser,
                null,
                null,
                cancellationToken);

            throw new UnauthorizedException("INVALID_CREDENTIALS", "Invalid credentials.");
        }

        if (adminUser.IsLocked(utcNow))
        {
            await auditLogService.RecordAsync(
                AuditActions.AdminAuthLocked,
                adminUser.Id,
                AuditActorType.Admin,
                AuditResourceTypes.AdminUser,
                adminUser.Id,
                null,
                cancellationToken);

            throw new UnauthorizedException("INVALID_CREDENTIALS", "Invalid credentials.");
        }

        if (!passwordHasher.Verify(request.Password, adminUser.PasswordHash))
        {
            adminUser.RecordFailedLogin(utcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await auditLogService.RecordAsync(
                AuditActions.AdminAuthLoginFailed,
                adminUser.Id,
                AuditActorType.Admin,
                AuditResourceTypes.AdminUser,
                adminUser.Id,
                null,
                cancellationToken);

            throw new UnauthorizedException("INVALID_CREDENTIALS", "Invalid credentials.");
        }

        adminUser.RecordSuccessfulLogin(utcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await auditLogService.RecordAsync(
            AuditActions.AdminAuthLogin,
            adminUser.Id,
            AuditActorType.Admin,
            AuditResourceTypes.AdminUser,
            adminUser.Id,
            null,
            cancellationToken);

        logger.LogInformation("Admin user {AdminUserId} authenticated successfully", adminUser.Id);

        if (!adminUser.MfaEnabled)
            return new AdminLoginResponse(null, true, false, adminUser.Id);

        return new AdminLoginResponse(null, false, true, adminUser.Id);
    }
}
