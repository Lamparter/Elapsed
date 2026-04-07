namespace Riverside.ResumableUploads;

public class TusProtocolException : Exception
{
	public TusProtocolException(string message) : base(message) { }
	public TusProtocolException(string message, Exception inner) : base(message, inner) { }
}
