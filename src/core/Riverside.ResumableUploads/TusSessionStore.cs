using System.Text;
using System.Text.Json;

namespace Riverside.ResumableUploads;

public sealed class TusSessionStore
{
	private readonly IModifiableFolder _root;
	private readonly JsonSerializerOptions _json;

	public TusSessionStore(IModifiableFolder root, JsonSerializerOptions? jsonOptions)
	{
		_root = root ?? throw new ArgumentNullException(nameof(root));
		_json = jsonOptions ?? new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			WriteIndented = false,
		};
	}

	public async Task<IFile?> TryGetSessionFileAsync(string fingerprint, CancellationToken ct)
	{
		var name = GetSessionFileName(fingerprint);

		await foreach (var item in _root.GetItemsAsync(StorableType.File).WithCancellation(ct).ConfigureAwait(false))
		{
			if (item is IFile f && string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase))
				return f;
		}

		return null;
	}

	public async Task<TusSession?> TryLoadAsync(string fingerprint, CancellationToken ct)
	{
		var file = await TryGetSessionFileAsync(fingerprint, ct).ConfigureAwait(false);
		if (file == null)
			return null;

		using var stream = await file.OpenReadAsync().ConfigureAwait(false);
		return await JsonSerializer.DeserializeAsync<TusSession>(stream, _json, ct).ConfigureAwait(false);
	}

	public async Task SaveAsync(TusSession session, CancellationToken ct)
	{
		if (session is null)
			throw new ArgumentNullException(nameof(session));

		session.UpdatedAt = DateTimeOffset.UtcNow;

		var mod = _root;
		//if (mod == null)
		//	throw new InvalidOperationException("Session cache root folder must implement IModifiableFolder");

		var existing = await TryGetSessionFileAsync(session.Fingerprint, ct).ConfigureAwait(false);
		var file = existing ?? await mod.CreateFileAsync(GetSessionFileName(session.Fingerprint), overwrite: true).ConfigureAwait(false);

		using var stream = await file.OpenWriteAsync().ConfigureAwait(false);
		await JsonSerializer.SerializeAsync(stream, session, _json, ct).ConfigureAwait(false);
		await stream.FlushAsync(ct).ConfigureAwait(false);
	}

	public async Task DeleteAsync(string fingerprint, CancellationToken ct)
	{
		var mod = _root;
		if (mod == null)
			return;

		var existing = await TryGetSessionFileAsync(fingerprint, ct).ConfigureAwait(false);
		if (existing != null)
			await mod.DeleteAsync((IStorableChild)existing).ConfigureAwait(false);
	}

	private static string SanitizeFileName(string input)
	{
		if (string.IsNullOrWhiteSpace(input))
			return "upload";

		var invalid = Path.GetInvalidFileNameChars();
		var sb = new StringBuilder(input.Length);
		foreach (var ch in input)
			sb.Append(Array.IndexOf(invalid, ch) >= 0 ? '_' : ch);

		return sb.ToString();
	}

	private static string GetSessionFileName(string fingerprint) => $"{SanitizeFileName(fingerprint)}.json";
}
