using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Riverside.Elapsed.CommandLine;

internal sealed class NoAuthProvider : IAuthenticationProvider
{
	public Task AuthenticateRequestAsync(
		RequestInformation request,
		Dictionary<string, object>? additionalAuthenticationContext = null,
		CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}
}
