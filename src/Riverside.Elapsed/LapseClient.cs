using System.Net.Http.Json;
using System.Text.Json;

namespace Riverside.Elapsed;

public class LapseClient
{
	// TODO: migrate to Riverside.Extensions.Primitives for HTTP when it is stable
	private readonly HttpClient _http;
	private readonly JsonSerializerOptions _jsonOptions;

	public LapseClient(HttpClient http, string? bearerToken = null)
	{
		_http = http;
		_http.BaseAddress ??= new Uri("https://api.lapse.hackclub.com/api");

		if (!string.IsNullOrWhiteSpace(bearerToken))
		{
			_http.DefaultRequestHeaders.Authorization
				= new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearerToken);
		}

		_jsonOptions = new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			PropertyNameCaseInsensitive = true,
		};
	}

	public void SetBearerToken(string token)
	{
		_http.DefaultRequestHeaders.Authorization
			= new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
	}

	private async Task<ApiResult<T>> SendAsync<T>(
		HttpRequestMessage request,
		CancellationToken cancellationToken = default)
	{
		using var response = await _http.SendAsync(request, cancellationToken);

		response.EnsureSuccessStatusCode();

		var envelope = await response.Content.ReadFromJsonAsync<ApiEnvelope<T>>(
			_jsonOptions,
			cancellationToken
		);

		if (envelope is null)
		{
			throw new InvalidOperationException("Empty response from Lapse API");
		}

		if (envelope.Ok)
		{
			return ApiResult<T>.Success(envelope.Data!);
		}

		return ApiResult<T>.Failure(envelope.Error!, envelope.Message!);
	}

	#region Convenience helpers for GET/PATCH
	protected Task<ApiResult<T>> GetAsync<T>(
		string path,
		CancellationToken cancellationToken = default)
	{
		var request = new HttpRequestMessage(HttpMethod.Get, path);
		return SendAsync<T>(request, cancellationToken);
	}

	protected Task<ApiResult<TResponse>> PatchAsync<TBody, TResponse>(
		string path,
		TBody body,
		CancellationToken cancellationToken = default)
	{
		var request = new HttpRequestMessage(HttpMethod.Patch, path)
		{
			Content = JsonContent.Create(body, options: _jsonOptions)
		};

		return SendAsync<TResponse>(request, cancellationToken);
	}
	#endregion
}
