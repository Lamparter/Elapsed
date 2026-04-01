namespace OwlCore.Storage.Tus;

public sealed class TusSession
{
	public required string Fingerprint { get; set; }
	public required Uri UploadUrl { get; set; }

	public required long UploadLength { get; set; }
	public long Offset { get; set; }

	public Dictionary<string, string> Metadata { get; set; } = [];

	public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
	public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
