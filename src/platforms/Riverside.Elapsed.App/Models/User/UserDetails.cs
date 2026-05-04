namespace Riverside.Elapsed.App.Models.User;

public sealed class UserDetails
{
	public string UserId;
	public DateTimeOffset CreatedAt;
	public string Handle;
	public string DisplayName;
	public Uri ProfilePictureUrl;
	public string Bio;
	public IReadOnlyList<Uri> Urls;
	public string? HackatimeId;
	public string? SlackId;
}
