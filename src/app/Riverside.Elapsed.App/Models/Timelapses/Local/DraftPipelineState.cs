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

	public Phase CurrentPhase;
	public double Progress;
	public string? LastError;
}
