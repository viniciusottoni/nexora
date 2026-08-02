using FluentValidation;

namespace Awaken.Application.Subscriptions.Commands.SyncEntitlement;

public class SyncEntitlementCommandValidator : AbstractValidator<SyncEntitlementCommand>
{
    public SyncEntitlementCommandValidator()
    {
        RuleFor(x => x.RevenueCatCustomerId).NotEmpty().MaximumLength(256);
    }
}
