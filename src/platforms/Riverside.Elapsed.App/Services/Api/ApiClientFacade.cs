using Microsoft.Kiota.Http.HttpClientLibrary;
using Riverside.Elapsed.App.Services.Auth;

namespace Riverside.Elapsed.App.Services.Api;

public sealed class ApiClientFacade(IAuthTokenStore tokenStore) : IApiClientFacade
{
	public Task<T?> SendAsync<T>(Func<ApiClient, Task<T?>> call, CancellationToken cancellationToken = default)
	{
		var authProvider = string.IsNullOrWhiteSpace(tokenStore.AccessToken)
			? new NoAuthProvider()
			: new StaticBearerAuthProvider(tokenStore.AccessToken);

		var adapter = new HttpClientRequestAdapter(authProvider)
		{
			BaseUrl = Riverside.Elapsed.Constants.Endpoint,
		};

		return call(new ApiClient(adapter));
	}
}
