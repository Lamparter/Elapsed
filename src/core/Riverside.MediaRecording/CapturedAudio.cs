namespace Riverside.MediaRecording;

/// <summary>
/// Represents the result of a completed audio recording session.
/// </summary>
public sealed record CapturedAudio : CapturedMedia
{
	/// <summary>
	/// Gets the effective audio format used by the recording.
	/// </summary>
	public AudioFormat? Format { get; init; }
}
