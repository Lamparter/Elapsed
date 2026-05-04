namespace Riverside.Elapsed.App.Services.Api;

public readonly record struct ApiResult<T>(T? Value, string? ErrorMessage, bool IsSuccess)
{
	public static ApiResult<T> Success(T value)
		=> new(value, null, true);

	public static ApiResult<T> Failure(string message)
		=> new(default, message, false);
}
