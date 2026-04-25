using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Riverside.Elapsed.CommandLine;

internal sealed class StaticBearerAuthProvider(string? token) : IAuthenticationProvider
{
	public Task AuthenticateRequestAsync(
		RequestInformation request,
		Dictionary<string, object>? additionalAuthenticationContext = null,
		CancellationToken cancellationToken = default)
	{
		if (!string.IsNullOrWhiteSpace(token))
		{
			request.Headers.TryAdd("Authorization", $"Bearer {token}");
		}

		return Task.CompletedTask;
	}
}
