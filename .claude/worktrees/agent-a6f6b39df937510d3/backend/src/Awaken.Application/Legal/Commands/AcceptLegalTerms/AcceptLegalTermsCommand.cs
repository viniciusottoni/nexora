using Awaken.Contracts.Legal;
using MediatR;

namespace Awaken.Application.Legal.Commands.AcceptLegalTerms;

public record AcceptLegalTermsCommand(
    string TermsVersion,
    string PrivacyVersion) : IRequest<LegalStatusResponse>;
