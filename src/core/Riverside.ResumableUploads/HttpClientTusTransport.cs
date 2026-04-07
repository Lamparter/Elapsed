using System.Net.Http.Headers;

namespace Riverside.ResumableUploads;

public sealed class HttpClientTusTransport : ITusTransport
{
	private readonly HttpClient _http;

	public HttpClientTusTransport(HttpClient httpClient)
	{
		_http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
	}

	public async Task<TusResponse> SendAsync(TusRequest request, CancellationToken cancellationToken)
	{
		if (request is null)
			throw new ArgumentNullException(nameof(request));

		using var msg = new HttpRequestMessage(request.Method, request.Url);

		foreach (var kvp in request.Headers)
			msg.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);

		if (request.Body != null)
		{
			var content = new StreamContent(request.Body);

			if (request.ContentType != null)
				content.Headers.ContentType = MediaTypeHeaderValue.Parse(request.ContentType);

			if (request.ContentLength.HasValue)
				content.Headers.ContentLength = request.ContentLength.Value;

			msg.Content = content;
		}

		var response = await _http.SendAsync(msg, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

		var result = new TusResponse
		{
			StatusCode = (int)response.StatusCode,
			Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
			Content = response.Content,
		};

		// Transfer ownership of the HttpResponseMessage to TusResponse so the response
		// (and its content stream) stays alive until the caller is done reading it.
		result.SetOwner(response);

		foreach (var header in response.Headers)
			result.Headers[header.Key] = string.Join(",", header.Value);

		if (response.Content != null)
		{
			foreach (var header in response.Content.Headers)
				result.Headers[header.Key] = string.Join(",", header.Value);
		}

		return result;
	}
}
