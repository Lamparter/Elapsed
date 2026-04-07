namespace Riverside.ResumableUploads;

public readonly struct TusOffsetInfo(long offset, long? uploadLength)
{
	public long Offset { get; } = offset;
	public long? UploadLength { get; } = uploadLength;
}
