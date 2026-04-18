namespace Riverside.MediaRecording;

/// <summary>
/// Represents a generic recording source.
/// It could be an audio stream, screenshot service, or screen recording.
/// </summary>
public interface IRecordable
{
	IReadOnlyList<RecordableDevice> Sources { get; }
}
