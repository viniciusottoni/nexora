using Awaken.Application.Common.Exceptions;
using Awaken.Application.Common.Interfaces;
using Awaken.Contracts.Legal;
using Awaken.Domain.Entities.Audit;
using Awaken.Domain.Entities.Auth;
using Awaken.Domain.Repositories;
using MediatR;

namespace Awaken.Application.Legal.Commands.AcceptLegalTerms;

public class AcceptLegalTermsCommandHandler(
    ICurrentUserService currentUser,
    IUserRepository userRepository,
    IDateTimeService dateTimeService,
    IUnitOfWork unitOfWork,
    IAuditLogService auditLogService) : IRequestHandler<AcceptLegalTermsCommand, LegalStatusResponse>
{
    public async Task<LegalStatusResponse> Handle(
        AcceptLegalTermsCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), userId);

        user.AcceptLegalTerms(request.TermsVersion, request.PrivacyVersion, dateTimeService.UtcNow);

        await auditLogService.RecordAsync(
            "legal_terms_accepted",
            userId,
            AuditActorType.User,
            "User",
            userId,
            $"{{\"termsVersion\":\"{request.TermsVersion}\",\"privacyVersion\":\"{request.PrivacyVersion}\"}}",
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
