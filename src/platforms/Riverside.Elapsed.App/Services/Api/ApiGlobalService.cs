using Riverside.Elapsed.App.Models.Global;
using Riverside.Elapsed.App.Models.Timelapses;
using Riverside.Elapsed.Global.ActiveUsers;
using Riverside.Elapsed.Global.RecentTimelapses;
using Riverside.Elapsed.Global.WeeklyLeaderboard;

namespace Riverside.Elapsed.App.Services.Api;

public sealed class ApiGlobalService(IApiClientFacade client) : IApiGlobalService
{
	public async Task<ApiResult<IReadOnlyList<LeaderboardEntry>>> GetWeeklyLeaderboardAsync(CancellationToken cancellationToken = default)
	{
		var response = await client.SendAsync(x => x.Global.WeeklyLeaderboard.GetAsWeeklyLeaderboardGetResponseAsync(cancellationToken: cancellationToken), cancellationToken);
		if (response?.WeeklyLeaderboardGetResponseMember1?.Data?.Leaderboard is { } leaderboard)
		{
			return ApiResult<IReadOnlyList<LeaderboardEntry>>.Success(leaderboard.Select(ApiMappingExtensions.MapLeaderboardEntry).ToArray());
		}

		var error = response?.WeeklyLeaderboardGetResponseMember2?.Message ?? "Failed to load leaderboard.";
		return ApiResult<IReadOnlyList<LeaderboardEntry>>.Failure(error);
	}

	public async Task<ApiResult<IReadOnlyList<Timelapse>>> GetRecentTimelapsesAsync(CancellationToken cancellationToken = default)
	{
		var response = await client.SendAsync(x => x.Global.RecentTimelapses.GetAsRecentTimelapsesGetResponseAsync(cancellationToken: cancellationToken), cancellationToken);
		if (response?.RecentTimelapsesGetResponseMember1?.Data?.Timelapses is { } timelapses)
		{
			return ApiResult<IReadOnlyList<Timelapse>>.Success(timelapses.Select(ApiMappingExtensions.MapTimelapse).ToArray());
		}

		var error = response?.RecentTimelapsesGetResponseMember2?.Message ?? "Failed to load timelapses.";
		return ApiResult<IReadOnlyList<Timelapse>>.Failure(error);
	}

	public async Task<ApiResult<ActiveUsers>> GetActiveUsersAsync(CancellationToken cancellationToken = default)
	{
		var response = await client.SendAsync(x => x.Global.ActiveUsers.GetAsActiveUsersGetResponseAsync(cancellationToken: cancellationToken), cancellationToken);
		if (response?.ActiveUsersGetResponseMember1?.Data?.Count is { } count)
		{
			return ApiResult<ActiveUsers>.Success(new ActiveUsers { Count = count });
		}

		var error = response?.ActiveUsersGetResponseMember2?.Message ?? "Failed to load active users.";
		return ApiResult<ActiveUsers>.Failure(error);
	}
}
