namespace Riverside.Elapsed.App.Models.Developer;

public class DeveloperApp
{
	public Guid AppId;
	public string Name;
	public string Description;
	public Uri HomepageUrl;
	public Uri? IconUrl;
	public IReadOnlyList<Uri> RedirectUris;
	public IReadOnlyList<string> Scopes;
	public TrustLevel TrustLevel;
	public string ClientId;
	public DateTimeOffset CreatedAt;
	public User.User? CreatedBy;
}
