namespace Riverside.Elapsed.App.Services.Build;

public sealed class BuildInfo
{
	public string DisplayVersion { get; init; } = string.Empty;
	public DateTimeOffset BuildTimestamp { get; init; }
	public string FullFooterText { get; init; } = string.Empty;
	public string WebFooterText { get; init; } = string.Empty;
}
