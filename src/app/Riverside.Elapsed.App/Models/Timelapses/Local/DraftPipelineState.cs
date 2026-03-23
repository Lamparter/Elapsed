namespace Riverside.Elapsed.App.Models.Timelapses.Local;

public class DraftPipelineState
{
	public enum Phase
	{
		LocalOnly,
		CreatingRemoteDraft,
		Encrypting,
		Uploading,
		ReadyToPublish,
		Publishing,
		Published,
		Error,
	}

	public double Progress;
	public string? LastError;
}
