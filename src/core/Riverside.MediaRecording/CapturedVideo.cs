namespace Riverside.MediaRecording;

/// <summary>
/// Represents the result of a completed video recording session.
/// </summary>
public sealed record CapturedVideo : CapturedMedia
{
	/// <summary>
	/// Gets the effective video format used by the recording.
	/// </summary>
	public CaptureFormat? Format { get; init; }

	/// <summary>
	/// Gets the captured region when region capture was requested.
	/// </summary>
	public CaptureRegion? Region { get; init; }

	/// <summary>
	/// Gets the effective optional audio configuration used by the recording.
	/// </summary>
	public AudioCaptureOptions? Audio { get; init; }
}
