namespace Riverside.Elapsed.App.Models.Developer;

public sealed class OAuthAppSummary
{
	public Guid AppId;
	public string Name;
	public string Description;
	public Uri IconUrl;
	public TrustLevel TrustLevel;
	public IReadOnlyList<string> Scopes;
}
