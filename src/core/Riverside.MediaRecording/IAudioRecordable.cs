using OwlCore.Storage;

namespace Riverside.MediaRecording;

/// <summary>
/// Represents an audio-capable recording provider.
/// </summary>
public interface IAudioRecordable : IRecordable, IAsyncDisposable
{
	/// <summary>
	/// Creates an audio recording session.
	/// </summary>
	Task<IAudioCaptureSession> CreateRecordingSessionAsync(
		RecordableDevice source,
		AudioFormat? format = null,
		IFile? outputFile = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets the active and completed sessions created by this provider.
	/// </summary>
	IReadOnlyList<IAudioCaptureSession> Sessions { get; }
}
