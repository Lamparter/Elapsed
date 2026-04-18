namespace Riverside.MediaRecording;

public interface IVideoCaptureSession
{
	Guid Id { get; }

	RecordingStatus Status { get; }

	TimeSpan Duration { get; }

	event EventHandler<> PreviewFrameAvailable;

	Task StartAsync(CancellationToken cancellationToken = default);

	Task PauseAsync();

	Task ResumeAsync();

	Task<> StopAsync();
}
