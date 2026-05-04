namespace Riverside.Elapsed.App.Models.Developer;

public sealed class OAuthGrantSummary
{
	public string GrantId;
	public string ServiceClientId;
	public string ServiceName;
	public IReadOnlyList<string> Scopes;
	public DateTimeOffset CreatedAt;
	public DateTimeOffset? LastUsedAt;
}
