using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Admin.Auth;
using Awaken.Domain.Entities.Admin;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Awaken.Application.Admin.Auth.Commands.AdminMfaVerify;

public class AdminMfaVerifyCommandHandler(
    IAdminUserRepository adminUserRepository,
    ITotpService totpService,
    IAdminJwtService adminJwtService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    IAuditLogService auditLogService,
    ILogger<AdminMfaVerifyCommandHandler> logger) : IRequestHandler<AdminMfaVerifyCommand, AdminMfaVerifyResponse>
{
    public async Task<AdminMfaVerifyResponse> Handle(AdminMfaVerifyCommand request, CancellationToken cancellationToken)
    {
        var adminUser = await adminUserRepository.GetByIdAsync(request.AdminUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(AdminUser), request.AdminUserId);

        var utcNow = dateTimeService.UtcNow;

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

            throw new UnauthorizedException("INVALID_MFA_CODE", "Invalid or expired MFA code.");
        }

        var isValid = !string.IsNullOrEmpty(adminUser.MfaSecretEncrypted)
            && totpService.VerifyCode(adminUser.MfaSecretEncrypted, request.Code);

        if (!isValid)
        {
            adminUser.RecordFailedLogin(utcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await auditLogService.RecordAsync(
                AuditActions.AdminAuthMfaFailed,
                adminUser.Id,
                AuditActorType.Admin,
                AuditResourceTypes.AdminUser,
                adminUser.Id,
                null,
                cancellationToken);

            throw new UnauthorizedException("INVALID_MFA_CODE", "Invalid or expired MFA code.");
        }

        if (!adminUser.MfaEnabled)
            adminUser.EnableMfa(utcNow);

        var token = adminJwtService.GenerateToken(adminUser);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await auditLogService.RecordAsync(
            AuditActions.AdminAuthLogin,
            adminUser.Id,
            AuditActorType.Admin,
            AuditResourceTypes.AdminUser,
            adminUser.Id,
            null,
            cancellationToken);

        logger.LogInformation("MFA verified for admin user {AdminUserId}", adminUser.Id);

        return new AdminMfaVerifyResponse(token);
    }
}
