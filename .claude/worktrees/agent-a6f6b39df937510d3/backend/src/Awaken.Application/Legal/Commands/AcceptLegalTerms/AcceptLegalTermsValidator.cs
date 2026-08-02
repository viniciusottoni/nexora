using FluentValidation;

namespace Awaken.Application.Legal.Commands.AcceptLegalTerms;

public class AcceptLegalTermsValidator : AbstractValidator<AcceptLegalTermsCommand>
{
    public AcceptLegalTermsValidator()
    {
        RuleFor(x => x.TermsVersion)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(x => x.PrivacyVersion)
            .NotEmpty()
            .MaximumLength(20);
    }
}
