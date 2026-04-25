using OwlCore.Storage;

namespace Riverside.MediaRecording;

/// <summary>
/// Represents a recording session lifecycle.
/// </summary>
/// <typeparam name="TResult">The capture result type returned by <see cref="StopAsync(CancellationToken)"/>.</typeparam>
public interface IRecordingSession<TResult>
	where TResult : CapturedMedia
{
	/// <summary>
	/// Gets the unique session identifier.
	/// </summary>
	Guid Id { get; }

	/// <summary>
	/// Gets the current lifecycle status.
	/// </summary>
	RecordingStatus Status { get; }

	/// <summary>
	/// Gets the elapsed recording duration.
	/// </summary>
	TimeSpan Duration { get; }

	/// <summary>
	/// Gets the currently selected source.
	/// </summary>
	RecordableDevice Source { get; }

	/// <summary>
	/// Gets the output file for this session.
	/// </summary>
	IFile? OutputFile { get; }

	/// <summary>
	/// Gets a value indicating whether this session can switch source at runtime.
	/// </summary>
	bool CanSwitchSource { get; }

	/// <summary>
	/// Starts recording.
	/// </summary>
	Task StartAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Pauses recording.
	/// </summary>
	Task PauseAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Resumes recording.
	/// </summary>
	Task ResumeAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Switches the recording source.
	/// </summary>
	Task SwitchSourceAsync(RecordableDevice source, CancellationToken cancellationToken = default);

	/// <summary>
	/// Stops recording and returns the captured artefact metadata.
	/// </summary>
	Task<TResult> StopAsync(CancellationToken cancellationToken = default);
}
