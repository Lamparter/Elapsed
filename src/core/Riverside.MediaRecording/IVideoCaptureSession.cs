namespace Riverside.MediaRecording;

/// <summary>
/// Represents a video recording session.
/// </summary>
public interface IVideoCaptureSession : IRecordingSession<CapturedVideo>
{
	/// <summary>
	/// Gets the preferred video format for this session.
	/// </summary>
	CaptureFormat? Format { get; }

	/// <summary>
	/// Gets the preferred region for this session.
	/// </summary>
	CaptureRegion? Region { get; }

	/// <summary>
	/// Gets the optional audio configuration for this session.
	/// </summary>
	AudioCaptureOptions? Audio { get; }
}
