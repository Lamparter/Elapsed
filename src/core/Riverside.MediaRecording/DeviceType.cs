namespace Riverside.MediaRecording;

/// <summary>
/// Identifies the category of a recordable source.
/// </summary>
public enum DeviceType
{
	/// <summary>
	/// The source type is unknown.
	/// </summary>
	Unknown,

	/// <summary>
	/// A physical or virtual display surface.
	/// </summary>
	Display,

	/// <summary>
	/// A single application window.
	/// </summary>
	Window,

	/// <summary>
	/// A region-defined source.
	/// </summary>
	Region,

	/// <summary>
	/// A camera input source.
	/// </summary>
	Camera,

	/// <summary>
	/// A microphone input source.
	/// </summary>
	Microphone,

	/// <summary>
	/// A speaker or playback endpoint.
	/// </summary>
	Speaker,

	/// <summary>
	/// A loopback source for system audio capture.
	/// </summary>
	Loopback,

	/// <summary>
	/// A virtual media source.
	/// </summary>
	Virtual,
}
