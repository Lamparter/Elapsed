namespace Riverside.Elapsed.App.Models.Timelapses;

public sealed class DraftTimelapseSummary
{
	public string DraftTimelapseId;
	public string Name;
	public string Description;
	public DateTimeOffset CreatedAt;
	public User.User Owner;
	public Guid DeviceId;
	public string IvHex;
	public Uri PreviewThumbnailUrl;
	public IReadOnlyList<Uri> Sessions;
	public IReadOnlyList<DraftEdit> EditList;
	public string? AssociatedTimelapseId;
}
