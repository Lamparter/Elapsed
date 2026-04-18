using OwlCore.Storage;

namespace Riverside.MediaRecording.Windows;

/// <summary>
/// An implementation of the video capture (screen recording) API for Windows.
/// </summary>
internal abstract class WindowsScreenCapture : IVideoCapturable
{
	public abstract IReadOnlyList<RecordableDevice> Sources { get; }

	public abstract IReadOnlyList<IVideoCaptureSession> Sessions { get; }

	public abstract Task<IVideoCaptureSession> CreateRecordingSessionAsync(
		RecordableDevice source,
		CaptureFormat? format = null,
		CaptureRegion? region = null,
		AudioCaptureOptions? audio = null,
		IFile? outputFile = null,
		CancellationToken cancellationToken = default);

	public abstract Task RefreshSourcesAsync(CancellationToken cancellationToken = default);

	public abstract ValueTask DisposeAsync();
}
