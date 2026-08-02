using FluentValidation;

namespace Nexora.Application.Installations.Commands.AuthenticateInstallationRequest;

public sealed class AuthenticateInstallationRequestCommandValidator
    : AbstractValidator<AuthenticateInstallationRequestCommand>
{
    public AuthenticateInstallationRequestCommandValidator()
    {
        RuleFor(x => x.InstallationId).NotEmpty();
        RuleFor(x => x.Timestamp).NotEmpty();
        RuleFor(x => x.Nonce).NotEmpty();
        RuleFor(x => x.Signature).NotEmpty();
        RuleFor(x => x.HttpMethod).NotEmpty();
        RuleFor(x => x.RequestPath).NotEmpty();

        // Mensagens de validação deliberadamente genéricas — este comando é um gate de
        // autenticação (ADR-021: erro de auth não deve ajudar quem está tentando adivinhar).
    }
}
