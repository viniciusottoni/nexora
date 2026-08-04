using FluentValidation;

namespace Nexora.Application.Alerts.Commands.SubscribePush;

internal sealed class SubscribePushCommandValidator : AbstractValidator<SubscribePushCommand>
{
    public SubscribePushCommandValidator()
    {
        RuleFor(c => c.Endpoint).NotEmpty().Must(e => Uri.TryCreate(e, UriKind.Absolute, out _))
            .WithMessage("O endpoint da assinatura de push deve ser uma URL válida.");
        RuleFor(c => c.P256dhKey).NotEmpty();
        RuleFor(c => c.AuthKey).NotEmpty();
    }
}
