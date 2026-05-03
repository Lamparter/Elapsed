namespace Riverside.Elapsed.App.Models.Timelapses;

public class Timelapse
{
	public string TimelapseId;
	public string Name;
	public string Description;
	public Visibility Visibility;
	public DateTimeOffset CreatedAt;
	public User.User Owner;
	public IReadOnlyList<Comment> Comments;
	public Uri? PlaybackUrl;
	public Uri? ThumbnailUrl;
	public double DurationSeconds;
	public string? HackatimeProject;
	public string? SourceDraftId;
}
