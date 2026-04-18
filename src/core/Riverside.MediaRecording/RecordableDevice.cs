namespace Riverside.MediaRecording;

/// <summary>
/// Represents a single recordable source with identity and capability metadata.
/// </summary>
/// <param name="Id">The unique source identifier.</param>
/// <param name="Name">A user-friendly source name for display.</param>
/// <param name="DeviceType">The source category.</param>
/// <param name="Capabilities">The source capabilities and supported formats.</param>
public readonly record struct RecordableDevice(
	Guid Id,
	string Name,
	DeviceType DeviceType,
	RecordableDeviceCapabilities Capabilities);
