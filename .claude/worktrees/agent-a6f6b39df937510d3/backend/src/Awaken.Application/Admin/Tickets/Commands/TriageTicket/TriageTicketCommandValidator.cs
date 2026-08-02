using FluentValidation;

namespace Awaken.Application.Admin.Tickets.Commands.TriageTicket;

public class TriageTicketCommandValidator : AbstractValidator<TriageTicketCommand>
{
    /// <summary>RN-005: fluxo controlado de status do ticket.</summary>
    public static readonly string[] ValidStatuses =
        ["open", "in_triagem", "in_andamento", "resolvido", "fechado"];

    public TriageTicketCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();

        RuleFor(x => x.Status)
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}")
            .When(x => x.Status is not null);

        RuleFor(x => x.Priority)
            .MaximumLength(32)
            .When(x => x.Priority is not null);
    }
}
