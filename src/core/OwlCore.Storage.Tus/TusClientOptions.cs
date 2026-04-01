using System.Text.Json;

namespace OwlCore.Storage.Tus;

public sealed class TusClientOptions
{
	public string TusVersion { get; set; } = "1.0.0";
	public int ChunkSizeBytes { get; set; } = 4 * 1024 * 1024;

	public required IModifiableFolder SessionCacheRoot { get; set; }

	public bool PersistAfterEachChunk { get; set; } = true;

	public JsonSerializerOptions? SessionJsonOptions { get; set; }
}
