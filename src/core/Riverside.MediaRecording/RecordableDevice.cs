namespace Riverside.MediaRecording;

public struct RecordableDevice
{
	/// <summary>
	/// The unique device identifier.
	/// </summary>
	public Guid Id { get; }

	/// <summary>
	/// A user-friendly device name for UI display, e.g. "Back camera".
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// The device category.
	/// </summary>
	public DeviceType DeviceType { get; }
}
