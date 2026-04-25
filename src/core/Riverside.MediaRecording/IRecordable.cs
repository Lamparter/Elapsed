namespace Riverside.MediaRecording;

/// <summary>
/// Represents a generic recording source.
/// It can expose cameras, displays, windows, microphones, speakers, and other media sources.
/// </summary>
public interface IRecordable
{
	/// <summary>
	/// Gets the currently available sources.
	/// </summary>
	IReadOnlyList<RecordableDevice> Sources { get; }

	/// <summary>
	/// Refreshes the source catalogue.
	/// </summary>
	Task RefreshSourcesAsync(CancellationToken cancellationToken = default);
}
