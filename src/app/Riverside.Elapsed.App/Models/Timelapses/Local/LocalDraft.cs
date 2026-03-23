namespace Riverside.Elapsed.App.Models.Timelapses.Local;

public class LocalDraft
{
	public Guid LocalDraftId;
	public string Name;
	public string Description;
	public DateTimeOffset CreatedAt;
	public DateTimeOffset LastModifiedAt;
	public Guid DeviceId;
	public IReadOnlyList<long> Snapshots; // milliseconds since epoch
	public List<DraftEdit> EditList;
	public List<LocalSession> Sessions;
	public LocalThumbnail Thumbnail;
	public RemoteDraftSync? Remote;
	public DraftPipelineState State;
}
