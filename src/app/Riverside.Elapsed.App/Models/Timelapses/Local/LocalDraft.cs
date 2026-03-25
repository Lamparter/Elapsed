namespace Riverside.Elapsed.App.Models.Timelapses.Local;

public sealed record LocalDraft
{
	public Guid LocalDraftId { get; init; }
	public string Name { get; init; } = "";
	public string Description { get; init; } = "";
	public DateTimeOffset CreatedAt { get; init; }
	public DateTimeOffset LastModifiedAt { get; init; }

	public Guid DeviceId { get; init; }

	public IReadOnlyList<long> Snapshots { get; init; } = []; // milliseconds since epoch
	public List<DraftEdit> EditList { get; init; } = [];

	public List<LocalSession> Sessions { get; init; } = [];
	public LocalThumbnail Thumbnail { get; init; } = new();

	public RemoteDraftSync? Remote { get; init; }
	public DraftPipelineState State { get; init; } = new();
}
