namespace Riverside.Elapsed.App.Services.Api;

public interface IApiClientFacade
{
	Task<T?> SendAsync<T>(Func<ApiClient, Task<T?>> call, CancellationToken cancellationToken = default);
}
