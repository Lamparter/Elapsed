namespace Riverside.MediaRecording;

public interface IVideoCapturable : ICapturable, IAsyncDisposable
{
	Task<IVideoCaptureSession> CreateRecordingSessionAsync(RecordableDevice device, CaptureFormat format, CaptureRegion? region, AudioFormat? audio);

	IReadOnlyList<IVideoCaptureSession> Sessions { get; }
}
