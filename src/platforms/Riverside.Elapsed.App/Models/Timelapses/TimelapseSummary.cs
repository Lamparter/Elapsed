namespace Riverside.Elapsed.App.Models.Timelapses;

public sealed class TimelapseSummary
{
	public string TimelapseId;
	public string Name;
	public string Description;
	public Visibility Visibility;
	public DateTimeOffset CreatedAt;
	public User.User Owner;
	public Uri? PlaybackUrl;
	public Uri? ThumbnailUrl;
	public double DurationSeconds;
}
