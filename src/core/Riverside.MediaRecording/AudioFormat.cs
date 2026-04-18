namespace Riverside.MediaRecording;

/// <summary>
/// Represents the preferred settings for audio capture.
/// Any <see langword="null"/> value indicates that the implementation should use an automatic default.
/// </summary>
public sealed record AudioFormat
{
	/// <summary>
	/// Gets the preferred sample rate in hertz.
	/// </summary>
	public int? SampleRate { get; init; }

	/// <summary>
	/// Gets the preferred channel count.
	/// </summary>
	public int? ChannelCount { get; init; }

	/// <summary>
	/// Gets the preferred audio codec name.
	/// </summary>
	public string? AudioCodec { get; init; }

	/// <summary>
	/// Gets the preferred audio bitrate in kilobits per second.
	/// </summary>
	public int? BitrateKbps { get; init; }
}
