using OwlCore.Storage;

namespace Riverside.MediaRecording;

/// <summary>
/// Represents the common result metadata produced by a completed recording session.
/// </summary>
public abstract record CapturedMedia
{
	/// <summary>
	/// Gets the resulting output file.
	/// </summary>
	public IFile? File { get; init; }

	/// <summary>
	/// Gets the UTC timestamp when recording started.
	/// </summary>
	public DateTimeOffset StartedAt { get; init; }

	/// <summary>
	/// Gets the UTC timestamp when recording ended.
	/// </summary>
	public DateTimeOffset EndedAt { get; init; }

	/// <summary>
	/// Gets the total recording duration.
	/// </summary>
	public TimeSpan Duration => EndedAt - StartedAt;
}
