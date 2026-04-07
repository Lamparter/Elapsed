namespace Riverside.ResumableUploads;

public sealed class TusRequest
{
	public required HttpMethod Method { get; set; }
	public required Uri Url { get; set; }

	public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	public Stream? Body { get; set; }

	public long? ContentLength { get; set; }
	public string? ContentType { get; set; }
}
