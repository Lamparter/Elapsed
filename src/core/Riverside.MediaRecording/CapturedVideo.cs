namespace Riverside.MediaRecording;

public struct CapturedVideo
{
	public string FilePath { get; set; }

	public MemoryStream? VideoStream { get; set; }

	public CaptureFormat Format { get; set; }

	public DateTimeOffset StartedAt { get; set; }

	public DateTimeOffset EndedAt { get; set; }
}
