using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Admin.Auth;
using Awaken.Domain.Entities.Admin;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Awaken.Application.Admin.Auth.Commands.AdminMfaSetup;

public class AdminMfaSetupCommandHandler(
    IAdminUserRepository adminUserRepository,
    ITotpService totpService,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    IAuditLogService auditLogService,
    ILogger<AdminMfaSetupCommandHandler> logger) : IRequestHandler<AdminMfaSetupCommand, AdminMfaSetupResponse>
{
    public async Task<AdminMfaSetupResponse> Handle(AdminMfaSetupCommand request, CancellationToken cancellationToken)
    {
        var adminUser = await adminUserRepository.GetByIdAsync(request.AdminUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(AdminUser), request.AdminUserId);

        var utcNow = dateTimeService.UtcNow;

        // Idempotent: reuse existing secret if MFA setup is already in progress
        string secret;
        if (!string.IsNullOrEmpty(adminUser.MfaSecretEncrypted) && !adminUser.MfaEnabled)
        {
            secret = adminUser.MfaSecretEncrypted;
        }
        else
        {
            secret = totpService.GenerateSecret();
            adminUser.SetMfaSecret(secret, utcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var uri = totpService.GetQrCodeUri(adminUser.Email, secret);
        var qrCodeBase64 = totpService.GenerateQrCodeBase64(uri);

        await auditLogService.RecordAsync(
            AuditActions.AdminAuthMfaSetup,
            adminUser.Id,
            AuditActorType.Admin,
            AuditResourceTypes.AdminUser,
            adminUser.Id,
            null,
            cancellationToken);

        logger.LogInformation("MFA setup initiated for admin user {AdminUserId}", adminUser.Id);

        return new AdminMfaSetupResponse(qrCodeBase64, secret);
    }
}
