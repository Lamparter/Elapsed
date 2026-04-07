namespace Riverside.ResumableUploads;

public sealed class TusOffsetMismatchException : TusProtocolException
{
	public TusOffsetMismatchException(string message) : base(message) { }
}
