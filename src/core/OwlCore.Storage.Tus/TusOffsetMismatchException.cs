namespace OwlCore.Storage.Tus;

public sealed class TusOffsetMismatchException : TusProtocolException
{
	public TusOffsetMismatchException(string message) : base(message) { }
}
