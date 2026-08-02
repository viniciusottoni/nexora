using FluentValidation;

namespace Awaken.Application.Auth.Commands.DeleteAccount;

public class DeleteAccountValidator : AbstractValidator<DeleteAccountCommand>
{
    public DeleteAccountValidator()
    {
    }
}
