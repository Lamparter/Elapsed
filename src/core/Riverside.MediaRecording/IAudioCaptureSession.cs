namespace Riverside.MediaRecording;

/// <summary>
/// Represents an audio recording session.
/// </summary>
public interface IAudioCaptureSession : IRecordingSession<CapturedAudio>
{
	/// <summary>
	/// Gets the preferred audio format for this session.
	/// </summary>
	AudioFormat? Format { get; }
}
