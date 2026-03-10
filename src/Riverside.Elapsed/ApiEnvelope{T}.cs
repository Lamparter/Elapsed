namespace Riverside.Elapsed;

// Raw envelope matching the API
public sealed class ApiEnvelope<T>
{
	public bool Ok { get; set; }
	public T? Data { get; set; }

	// Error shape when ok == false
	public string? Error { get; set; }
	public string? Message { get; set; }
}
