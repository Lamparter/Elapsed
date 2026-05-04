namespace Riverside.Elapsed.App.Models.Auth;

public sealed class OAuthToken
{
	public string AccessToken;
	public string TokenType;
	public double ExpiresIn;
	public string Scope;
	public string? RefreshToken;
}
