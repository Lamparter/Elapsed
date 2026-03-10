namespace Riverside.Elapsed.Users;

public class UserClient : LapseClient
{
	public UserClient(HttpClient http, string? bearerToken = null)
		: base(http, bearerToken)
	{
	}

	// GET /user/myself
	public Task<ApiResult<MyselfData>> GetMyselfAsync(
		CancellationToken cancellationToken = default)
	{
		return GetAsync<MyselfData>("/user/myself", cancellationToken);
	}

	// GET /user/query?id=...&handle=...&hackatimeId=...
	public Task<ApiResult<UserQueryData>> QueryUserAsync(
		string? id = null,
		string? handle = null,
		long? hackatimeId = null,
		CancellationToken cancellationToken = default)
	{
		var query = BuildQueryString(id, handle, hackatimeId);
		return GetAsync<UserQueryData>($"/user/query{query}", cancellationToken);
	}

	// PATCH /user/update
	public Task<ApiResult<UserUpdateData>> UpdateUserAsync(
		string id,
		UserUpdateChanges changes,
		CancellationToken cancellationToken = default)
	{
		var body = new UserUpdateRequest
		{
			Id = id,
			Changes = changes
		};

		return PatchAsync<UserUpdateRequest, UserUpdateData>(
			"/user/update",
			body,
			cancellationToken);
	}

	private static string BuildQueryString(string? id, string? handle, long? hackatimeId)
	{
		var query = System.Web.HttpUtility.ParseQueryString(string.Empty);

		if (!string.IsNullOrWhiteSpace(id))
			query["id"] = id;
		if (!string.IsNullOrWhiteSpace(handle))
			query["handle"] = handle;
		if (hackatimeId.HasValue)
			query["hackatimeId"] = hackatimeId.Value.ToString();

		var qs = query.ToString();
		return string.IsNullOrEmpty(qs) ? string.Empty : "?" + qs;
	}
}
