using Nexora.Application.Abstractions.Messaging;
using Nexora.Application.Abstractions.Persistence;
using Nexora.Domain.Platform;
using Nexora.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Nexora.Application.Onboarding.Commands.CompleteOnboardingStep;

internal sealed class CompleteOnboardingStepCommandHandler : IRequestHandler<CompleteOnboardingStepCommand, Result>
{
    private readonly IApplicationDbContext _db;

    public CompleteOnboardingStepCommandHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<Result> Handle(CompleteOnboardingStepCommand request, CancellationToken cancellationToken)
    {
        if (request.Key == OnboardingStepKey.Activation)
        {
            return Result.Failure(
                "A ativação é feita pelo próprio endpoint de ativação, depois que os demais passos estiverem concluídos.",
                ApiErrorCodes.ValidationError);
        }

        var step = await _db.OnboardingSteps
            .SingleOrDefaultAsync(s => s.TenantId == request.TenantId && s.Key == request.Key, cancellationToken);

        if (step is null)
        {
            return Result.Failure("Passo do roteiro de implantação não encontrado.", ApiErrorCodes.OnboardingStepNotFound);
        }

        step.Complete(DateTimeOffset.UtcNow, request.CompletedBy);

        return Result.Success();
    }
}
