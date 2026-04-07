using System.Globalization;
using System.Net;
using System.Text;

namespace Riverside.ResumableUploads;

public sealed class TusClient
{
	private readonly ITusTransport _transport;
	private readonly TusClientOptions _options;
	private readonly TusSessionStore _sessions;

	public TusClient(ITusTransport transport, TusClientOptions options)
	{
		_transport = transport ?? throw new ArgumentNullException(nameof(transport));
		_options = options ?? throw new ArgumentNullException(nameof(options));

		if (_options.SessionCacheRoot == null)
			throw new ArgumentException("SessionCacheRoot is required.", nameof(options));

		if (_options.ChunkSizeBytes <= 0)
			throw new ArgumentOutOfRangeException(nameof(options.ChunkSizeBytes));

		_sessions = new TusSessionStore(_options.SessionCacheRoot, _options.SessionJsonOptions);
	}

	public async Task<TusSession> CreateOrResumeAsync(
		string fingerprint,
		IFile sourceFile,
		Uri creationEndpoint,
		Dictionary<string, string>? metadata = null,
		CancellationToken ct = default)
	{
		if (string.IsNullOrWhiteSpace(fingerprint))
			throw new ArgumentException("Fingerprint is required.", nameof(fingerprint));
		if (sourceFile is null)
			throw new ArgumentNullException(nameof(sourceFile));
		if (creationEndpoint is null)
			throw new ArgumentNullException(nameof(creationEndpoint));

		var existing = await _sessions.TryLoadAsync(fingerprint, ct).ConfigureAwait(false);
		if (existing != null)
		{
			var head = await HeadAsync(existing.UploadUrl, ct).ConfigureAwait(false);
			existing.Offset = head.Offset;
			existing.UploadLength = head.UploadLength ?? existing.UploadLength;

			await _sessions.SaveAsync(existing, ct).ConfigureAwait(false);
			return existing;
		}

		var size = await TryGetFileSizeAsync(sourceFile).ConfigureAwait(false);
		if (size == null)
			throw new InvalidOperationException("Upload-Length is required. Could not determine source file size.");

		var session = await CreateAsync(fingerprint, creationEndpoint, size.Value, metadata ?? [], ct).ConfigureAwait(false);
		await _sessions.SaveAsync(session, ct).ConfigureAwait(false);
		return session;
	}

	public async Task<TusSession> CreateAsync(
		string fingerprint,
		Uri creationEndpoint,
		long uploadLength,
		Dictionary<string, string> metadata,
		CancellationToken ct = default)
	{
		var headers = BaseTusHeaders();
		headers["Upload-Length"] = uploadLength.ToString(CultureInfo.InvariantCulture);

		var metaHeader = EncodeMetadata(metadata);
		if (!string.IsNullOrEmpty(metaHeader))
			headers["Upload-Metadata"] = metaHeader;

		using var response = await _transport.SendAsync(new TusRequest
		{
			Method = HttpMethod.Post,
			Url = creationEndpoint,
			Headers = headers,
		}, ct).ConfigureAwait(false);

		ValidateTusResumableIfPresent(response);

		if (response.StatusCode < 200 || response.StatusCode >= 300)
			throw new TusProtocolException($"TUS create failed with HTTP {response.StatusCode}.");

		if (!response.Headers.TryGetValue("Location", out var location) || string.IsNullOrWhiteSpace(location))
			throw new TusProtocolException("TUS create response missing Location header.");

		var uploadUrl = ResolveLocation(creationEndpoint, location);

		return new TusSession
		{
			Fingerprint = fingerprint,
			UploadUrl = uploadUrl,
			UploadLength = uploadLength,
			Offset = 0,
			Metadata = metadata ?? [],
			CreatedAt = DateTimeOffset.UtcNow,
			UpdatedAt = DateTimeOffset.UtcNow,
		};
	}

	public async Task<TusOffsetInfo> HeadAsync(Uri uploadUrl, CancellationToken ct = default)
	{
		var headers = BaseTusHeaders();

		using var response = await _transport.SendAsync(new TusRequest
		{
			Method = HttpMethod.Head,
			Url = uploadUrl,
			Headers = headers,
		}, ct).ConfigureAwait(false);

		ValidateTusResumableIfPresent(response);

		if (response.StatusCode == (int)HttpStatusCode.NotFound)
			throw new TusProtocolException("TUS upload URL not found (404).");

		if (response.StatusCode < 200 || response.StatusCode >= 300)
			throw new TusProtocolException($"TUS HEAD failed with HTTP {response.StatusCode}.");

		if (!TryGetInt64Header(response, "Upload-Offset", out var offset))
			throw new TusProtocolException("TUS HEAD missing Upload-Offset.");

		long? length = null;
		if (TryGetInt64Header(response, "Upload-Length", out var uploadLength))
			length = uploadLength;

		return new TusOffsetInfo(offset, length);
	}

	public async Task UploadAsync(
		TusSession session,
		IFile sourceFile,
		IProgress<TusUploadProgress>? progress = null,
		CancellationToken ct = default)
	{
		if (session is null)
			throw new ArgumentNullException(nameof(session));
		if (sourceFile is null)
			throw new ArgumentNullException(nameof(session));

		var total = session.UploadLength;
		long bytesSentThisCall = 0;

		while (session.Offset < total)
		{
			ct.ThrowIfCancellationRequested();

			var remaining = total - session.Offset;
			var chunkSize = (int)Math.Min(_options.ChunkSizeBytes, remaining);

			using var src = await sourceFile.OpenReadAsync().ConfigureAwait(false);
			if (!src.CanSeek)
				throw new NotSupportedException("The source file stream must be seekable to upload from arbitrary offsets. Add a cache/materialise step for non-seekable sources.");

			src.Seek(session.Offset, SeekOrigin.Begin);

			using var limited = new ReadOnlySubStream(src, chunkSize);

			TusOffsetInfo patchInfo;
			try
			{
				patchInfo = await PatchAsync(session.UploadUrl, session.Offset, limited, chunkSize, ct).ConfigureAwait(false);
			}
			catch (TusOffsetMismatchException)
			{
				var head = await HeadAsync(session.UploadUrl, ct).ConfigureAwait(false);
				session.Offset = head.Offset;
				await _sessions.SaveAsync(session, ct).ConfigureAwait(false);
				continue;
			}

			session.Offset = patchInfo.Offset;
			bytesSentThisCall += chunkSize;

			progress?.Report(new TusUploadProgress(bytesSentThisCall, total, session.Offset));

			if (_options.PersistAfterEachChunk)
				await _sessions.SaveAsync(session, ct).ConfigureAwait(false);
		}

		await _sessions.SaveAsync(session, ct).ConfigureAwait(false);
	}

	private async Task<TusOffsetInfo> PatchAsync(Uri uploadUrl, long uploadOffset, Stream body, int bodyLength, CancellationToken ct)
	{
		var headers = BaseTusHeaders();
		headers["Upload-Offset"] = uploadOffset.ToString(CultureInfo.InvariantCulture);

		using var response = await _transport.SendAsync(new TusRequest
		{
			Method = new HttpMethod("PATCH"),
			Url = uploadUrl,
			Headers = headers,
			Body = body,
			ContentLength = bodyLength,
			ContentType = "application/offset+octect-stream",
		}, ct).ConfigureAwait(false);

		ValidateTusResumableIfPresent(response);

		if (response.StatusCode == 409)
			throw new TusOffsetMismatchException("TUS PATCH conflict (409). Offset mismatch.");

		if (response.StatusCode < 200 || response.StatusCode >= 300)
			throw new TusProtocolException($"TUS PATCH failed with HTTP {response.StatusCode}.");

		if (!TryGetInt64Header(response, "Upload-Offset", out var newOffset))
			throw new TusProtocolException("TUS PATCH response missing Upload-Offset.");

		if (newOffset < uploadOffset)
			throw new TusOffsetMismatchException("Server returned an Upload-Offset lower than request offset.");

		return new TusOffsetInfo(newOffset, uploadLength: null);
	}

	private Dictionary<string, string> BaseTusHeaders()
	{
		return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["Tus-Resumable"] = _options.TusVersion
		};
	}

	private static void ValidateTusResumableIfPresent(TusResponse response)
	{
		if (response.Headers.TryGetValue("Tus-Resumable", out var v) && 
			!string.Equals(v?.Trim(), "1.0.0", StringComparison.Ordinal))
		{
			throw new TusProtocolException($"Unexpected Tus-Resumable response header: '{v}'.");
		}
	}

	private static bool TryGetInt64Header(TusResponse response, string header, out long value)
	{
		value = 0;

		if (!response.Headers.TryGetValue(header, out var str) ||
			string.IsNullOrWhiteSpace(str))
		{
			return false;
		}

		return long.TryParse(str.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
	}

	private static Uri ResolveLocation(Uri requestUri, string location)
	{
		if (Uri.TryCreate(location, UriKind.Absolute, out var abs))
			return abs;

		return new Uri(requestUri, location);
	}

	private static string EncodeMetadata(Dictionary<string, string>? metadata)
	{
		if (metadata == null || metadata.Count == 0)
			return string.Empty;

		var parts = new List<string>(metadata.Count);
		foreach (var kvp in metadata)
		{
			var key = kvp.Key?.Trim();
			if (string.IsNullOrEmpty(key))
				continue;

			var val = kvp.Value ?? string.Empty;
			var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(val));
			parts.Add($"{key} {b64}");
		}

		return string.Join(",", parts);
	}

	private static async Task<long?> TryGetFileSizeAsync(IFile file)
	{
		try
		{
			using var s = await file.OpenReadAsync().ConfigureAwait(false);
			if (s.CanSeek)
				return s.Length;
		}
		catch { }

		return null;
	}
}
