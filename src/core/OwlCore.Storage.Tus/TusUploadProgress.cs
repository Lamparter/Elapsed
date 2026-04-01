namespace OwlCore.Storage.Tus;

public sealed class TusUploadProgress(long bytesSentThisCall, long totalBytes, long offset)
{
	public long BytesSentThisCall { get; } = bytesSentThisCall;
	public long TotalBytes { get; } = totalBytes;
	public long Offset { get; } = offset;
}
