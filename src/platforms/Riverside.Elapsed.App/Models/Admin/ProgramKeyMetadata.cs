namespace Riverside.Elapsed.App.Models.Admin;

public sealed class ProgramKeyMetadata
{
	public Guid KeyId;
	public string Name;
	public string KeyPrefix;
	public IReadOnlyList<string> Scopes;
	public User.UserSummary CreatedBy;
	public DateTimeOffset CreatedAt;
	public DateTimeOffset? LastUsedAt;
	public DateTimeOffset? RevokedAt;
	public DateTimeOffset ExpiresAt;
}
