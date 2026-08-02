namespace Awaken.Contracts.Auth;

public record LoginUserRequest(
    string Email = "",
    string Password = "");
