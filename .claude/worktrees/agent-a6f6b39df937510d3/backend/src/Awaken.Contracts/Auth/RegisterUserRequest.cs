namespace Awaken.Contracts.Auth;

public record RegisterUserRequest
{
    public RegisterUserRequest()
    {
    }

    public RegisterUserRequest(string? name, string? email, string? password, string language = "pt-BR")
    {
        Name = name;
        Email = email;
        Password = password;
        Language = language;
    }

    public string? Name { get; init; }
    public string? Email { get; init; }
    public string? Password { get; init; }
    public string Language { get; init; } = "pt-BR";
}
