using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Legal;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Legal.Commands.AcceptResponsibilityNotice;

public class AcceptResponsibilityNoticeCommandHandler(
    ICurrentUserService currentUser,
    IUserRepository userRepository,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    IAuditLogService auditLogService) : IRequestHandler<AcceptResponsibilityNoticeCommand, LegalStatusResponse>
{
    public async Task<LegalStatusResponse> Handle(
        AcceptResponsibilityNoticeCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), userId);

        user.AcceptResponsibilityNotice(request.NoticeVersion, dateTimeService.UtcNow);

        await auditLogService.RecordAsync(
            "responsibility_notice_accepted",
            userId,
            AuditActorType.User,
            "User",
            userId,
            $"{{\"noticeVersion\":\"{request.NoticeVersion}\"}}",
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new LegalStatusResponse(
            user.HasAcceptedLegal,
            user.HasAcceptedResponsibilityNotice,
            user.TermsVersion,
            user.PrivacyVersion,
            user.ResponsibilityNoticeVersion,
            user.TermsAcceptedAt,
            user.PrivacyAcceptedAt,
            user.ResponsibilityNoticeAcceptedAt);
    }
}
