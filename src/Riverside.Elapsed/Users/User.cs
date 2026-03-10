namespace Riverside.Elapsed.Users;

public sealed class User
{
	public string Id { get; set; } = default!;
	public long CreatedAt { get; set; }

	public string Handle { get; set; } = default!;
	public string DisplayName { get; set; } = default!;
	public string ProfilePictureUrl { get; set; } = default!;

	public string Bio { get; set; } = string.Empty;
	public List<string> Urls { get; set; } = new();

	public string? HackatimeId { get; set; }
	public string? SlackId { get; set; }

	public PrivateUserData? Private { get; set; }
}
