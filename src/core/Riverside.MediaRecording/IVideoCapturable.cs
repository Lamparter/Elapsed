using OwlCore.Storage;

namespace Riverside.MediaRecording;

/// <summary>
/// Represents a video-capable recording provider.
/// </summary>
public interface IVideoCapturable : ICapturable, IAsyncDisposable
{
	/// <summary>
	/// Creates a video recording session.
	/// </summary>
	Task<IVideoCaptureSession> CreateRecordingSessionAsync(
		RecordableDevice source,
		CaptureFormat? format = null,
		CaptureRegion? region = null,
		AudioCaptureOptions? audio = null,
		IFile? outputFile = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets the active and completed sessions created by this provider.
	/// </summary>
	IReadOnlyList<IVideoCaptureSession> Sessions { get; }
}
