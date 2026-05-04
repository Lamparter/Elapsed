using Riverside.Elapsed.App.Models.Global;
using Riverside.Elapsed.App.Models.Timelapses;

namespace Riverside.Elapsed.App.Services.Api;

public interface IApiGlobalService
{
	Task<ApiResult<IReadOnlyList<LeaderboardEntry>>> GetWeeklyLeaderboardAsync(CancellationToken cancellationToken = default);
	Task<ApiResult<IReadOnlyList<Riverside.Elapsed.App.Models.Timelapses.Timelapse>>> GetRecentTimelapsesAsync(CancellationToken cancellationToken = default);
	Task<ApiResult<ActiveUsers>> GetActiveUsersAsync(CancellationToken cancellationToken = default);
}
