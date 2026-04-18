namespace Riverside.MediaRecording;

/// <summary>
/// Represents optional audio configuration for a video recording session.
/// </summary>
public sealed record AudioCaptureOptions
{
	/// <summary>
	/// Gets a value indicating whether microphone audio should be included.
	/// </summary>
	public bool IncludeMicrophone { get; init; } = true;

	/// <summary>
	/// Gets a value indicating whether system playback audio should be included.
	/// </summary>
	public bool IncludeSystemAudio { get; init; }

	/// <summary>
	/// Gets the preferred microphone source.
	/// </summary>
	public RecordableDevice? MicrophoneSource { get; init; }

	/// <summary>
	/// Gets the preferred system audio source.
	/// </summary>
	public RecordableDevice? SystemAudioSource { get; init; }

	/// <summary>
	/// Gets the preferred audio format.
	/// </summary>
	public AudioFormat? Format { get; init; }
}
