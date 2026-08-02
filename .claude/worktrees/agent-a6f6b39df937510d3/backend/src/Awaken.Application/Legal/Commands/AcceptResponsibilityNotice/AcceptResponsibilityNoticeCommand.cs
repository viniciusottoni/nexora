using Awaken.Contracts.Legal;
using MediatR;

namespace Awaken.Application.Legal.Commands.AcceptResponsibilityNotice;

public record AcceptResponsibilityNoticeCommand(
    string NoticeVersion) : IRequest<LegalStatusResponse>;
