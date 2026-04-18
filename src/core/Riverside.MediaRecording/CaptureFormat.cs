namespace Riverside.MediaRecording;

/// <summary>
/// Represents the preferred settings for video capture.
/// Any <see langword="null"/> value indicates that the implementation should use an automatic default.
/// </summary>
public sealed record CaptureFormat
{
	/// <summary>
	/// Gets the preferred output width.
	/// </summary>
	public int? Width { get; init; }

	/// <summary>
	/// Gets the preferred output height.
	/// </summary>
	public int? Height { get; init; }

	/// <summary>
	/// Gets the preferred frame rate.
	/// </summary>
	public double? FrameRate { get; init; }

	/// <summary>
	/// Gets the preferred pixel format, for example <c>NV12</c> or <c>BGRA8</c>.
	/// </summary>
	public string? PixelFormat { get; init; }

	/// <summary>
	/// Gets the preferred video codec name.
	/// </summary>
	public string? VideoCodec { get; init; }

	/// <summary>
	/// Gets the preferred video bitrate in kilobits per second.
	/// </summary>
	public int? BitrateKbps { get; init; }
}
