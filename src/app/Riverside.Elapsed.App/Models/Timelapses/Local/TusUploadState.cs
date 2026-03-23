namespace Riverside.Elapsed.App.Models.Timelapses.Local;

public class TusUploadState
{
	public string? UploadUrl;
	public long BytesUploaded;
	public long BytesTotal;
	public bool IsComplete;
	public string? LastError;
	public DateTimeOffset StartedAt;
	public DateTimeOffset? CompletedAt;
}
