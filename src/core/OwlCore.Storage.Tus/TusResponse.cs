using System.Net.Http.Json;
using System.Text.Json;

namespace OwlCore.Storage.Tus;

public sealed class TusResponse : IDisposable
{
	public required int StatusCode { get; set; }
	public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	public HttpContent? Content { get; set; }

	public Task<T?> ReadJsonAsync<T>(JsonSerializerOptions? options, CancellationToken ct)
	{
		if (Content == null)
			throw new InvalidOperationException("Response has no HttpContent. Transport did not provide a content object.");

		return Content.ReadFromJsonAsync<T>(options, ct);
	}

	public void Dispose()
	{
		Content?.Dispose();
	}
}
