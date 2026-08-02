namespace Awaken.Contracts.Auth;

public record DeleteAccountRequest(string Confirmation)
{
    public const string ExpectedConfirmation = "DELETE_MY_ACCOUNT";
}
