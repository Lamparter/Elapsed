using System.Text.Json;
using System.Text.Json.Serialization;

namespace Riverside.Elapsed.App;

public static class Constants
{
	public static JsonSerializerOptions SerializerOptions = new()
	{
		WriteIndented = true,
		PropertyNameCaseInsensitive = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		IgnoreReadOnlyProperties = true,
		ReferenceHandler = ReferenceHandler.IgnoreCycles,
	};
}
