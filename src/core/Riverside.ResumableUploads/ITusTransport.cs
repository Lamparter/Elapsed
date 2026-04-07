namespace Riverside.ResumableUploads;

public interface ITusTransport
{
	Task<TusResponse> SendAsync(TusRequest request, CancellationToken cancellationToken);
}
