using System.Net.Http.Json;
using System.Text.Json;

namespace Riverside.ResumableUploads;

public sealed class TusResponse : IDisposable
{
	private IDisposable? _owner;

	public required int StatusCode { get; set; }
	public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	public HttpContent? Content { get; set; }

	/// <summary>
	/// Sets the owner of this response. The owner is disposed when this response is disposed,
	/// which ensures the underlying HTTP connection is released at the right time.
	/// </summary>
	internal void SetOwner(IDisposable owner) => _owner = owner;

	public Task<T?> ReadJsonAsync<T>(JsonSerializerOptions? options, CancellationToken ct)
	{
		if (Content == null)
			throw new InvalidOperationException("Response has no HttpContent. Transport did not provide a content object.");

		return Content.ReadFromJsonAsync<T>(options, ct);
	}

	public void Dispose()
	{
		// If an owner (e.g. HttpResponseMessage) is set, disposing it also disposes its Content.
		// Otherwise fall back to directly disposing Content.
		if (_owner != null)
			_owner.Dispose();
		else
			Content?.Dispose();
	}
}
