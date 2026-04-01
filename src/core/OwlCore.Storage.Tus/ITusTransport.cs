namespace OwlCore.Storage.Tus;

public interface ITusTransport
{
	Task<TusResponse> SendAsync(TusRequest request, CancellationToken cancellationToken);
}
