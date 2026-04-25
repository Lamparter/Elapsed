namespace Riverside.MediaRecording;

/// <summary>
/// Describes the capture capabilities exposed by a recording source.
/// </summary>
public sealed record RecordableDeviceCapabilities
{
	/// <summary>
	/// Gets a reusable empty capabilities instance.
	/// </summary>
	public static RecordableDeviceCapabilities None { get; } = new();

	/// <summary>
	/// Gets a value indicating whether video capture is supported.
	/// </summary>
	public bool SupportsVideoCapture { get; init; }

	/// <summary>
	/// Gets a value indicating whether audio capture is supported.
	/// </summary>
	public bool SupportsAudioCapture { get; init; }

	/// <summary>
	/// Gets a value indicating whether region-restricted capture is supported.
	/// </summary>
	public bool SupportsRegionCapture { get; init; }

	/// <summary>
	/// Gets a value indicating whether source switching is supported while recording.
	/// </summary>
	public bool SupportsSourceSwitching { get; init; }

	/// <summary>
	/// Gets the supported video formats.
	/// </summary>
	public IReadOnlyList<CaptureFormat> SupportedVideoFormats { get; init; } = [];

	/// <summary>
	/// Gets the supported audio formats.
	/// </summary>
	public IReadOnlyList<AudioFormat> SupportedAudioFormats { get; init; } = [];

	/// <summary>
	/// Gets the default video format.
	/// </summary>
	public CaptureFormat? DefaultVideoFormat { get; init; }

	/// <summary>
	/// Gets the default audio format.
	/// </summary>
	public AudioFormat? DefaultAudioFormat { get; init; }
}
