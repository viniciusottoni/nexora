namespace Awaken.Contracts.Auth;

public record GoogleSignInRequest(
    string Provider = "google",
    string ProviderCredential = "");
