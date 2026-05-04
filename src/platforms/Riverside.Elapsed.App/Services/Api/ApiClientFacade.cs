using Microsoft.Kiota.Abstractions;

namespace Riverside.Elapsed.App.Services.Api;

public sealed class ApiClientFacade(ApiClient client) : IApiClientFacade
{
	public Task<T?> SendAsync<T>(Func<ApiClient, Task<T?>> call, CancellationToken cancellationToken = default)
		=> call(client);
}
