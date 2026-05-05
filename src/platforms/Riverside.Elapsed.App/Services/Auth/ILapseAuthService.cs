using Riverside.Elapsed.App.Services.Api;

namespace Riverside.Elapsed.App.Services.Auth;

public interface ILapseAuthService
{
	event EventHandler? LoggedOut;

	bool IsAuthenticated { get; }

	Task<bool> TryRestoreSessionAsync(CancellationToken cancellationToken = default);
	Task<ApiResult<bool>> LoginAsync(CancellationToken cancellationToken = default);
	Task LogoutAsync(CancellationToken cancellationToken = default);
}
