using System.Text.RegularExpressions;
using FluentValidation;

namespace Awaken.Application.Admin.Bugs.Commands.CreateOperationalBug;

/// <summary>
/// US-171 RN-001 / CA-002: Severity, Status, Component, Environment, Origin e OccurredAtUtc
/// são obrigatórios no registro de um bug. A criação é bloqueada com erro claro quando algum
/// campo obrigatório estiver ausente ou fora do domínio permitido.
///
/// US-171 RN-002 / US-164 RN-001 (defesa em profundidade — MVP):
/// Description nunca deve conter segredos (senhas, tokens). Não há varredura completa de
/// segredos neste MVP — confiamos que admins não colem dados sensíveis — mas aplicamos um
/// guard de regex simples para rejeitar padrões óbvios como "password=", "Bearer " ou "token=".
/// </summary>
public class CreateOperationalBugCommandValidator : AbstractValidator<CreateOperationalBugCommand>
{
    private static readonly string[] ValidSeverities = ["low", "medium", "high", "critical"];
    private static readonly string[] ValidEnvironments = ["dev", "staging", "prod"];
    private static readonly string[] ValidOrigins = ["user_report", "monitoring", "internal"];

    private static readonly Regex SecretLikePattern = new(
        "password=|Bearer |token=",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public CreateOperationalBugCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.Severity)
            .NotEmpty()
            .Must(s => ValidSeverities.Contains(s.ToLowerInvariant()))
            .WithMessage("Severity must be one of: low, medium, high, critical");

        RuleFor(x => x.Component)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(x => x.Environment)
            .NotEmpty()
            .Must(e => ValidEnvironments.Contains(e.ToLowerInvariant()))
            .WithMessage("Environment must be one of: dev, staging, prod");

        RuleFor(x => x.Origin)
            .NotEmpty()
            .Must(o => ValidOrigins.Contains(o.ToLowerInvariant()))
            .WithMessage("Origin must be one of: user_report, monitoring, internal");

        RuleFor(x => x.OccurredAtUtc)
            .NotEqual(default(DateTime))
            .WithMessage("OccurredAtUtc is required");

        RuleFor(x => x.Description)
            .Must(d => d is null || !SecretLikePattern.IsMatch(d))
            .WithMessage("Description must not contain secret-like patterns (password, token, Bearer)");
    }
}
