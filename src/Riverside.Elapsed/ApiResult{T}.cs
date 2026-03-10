namespace Riverside.Elapsed;

public sealed class ApiResult<T>
{
	public bool IsSuccess { get; }
	public T? Data { get; }
	public string? Error { get; }
	public string? Message { get; }

	private ApiResult(bool isSuccess, T? data, string? error, string? message)
	{
		IsSuccess = isSuccess;
		Data = data;
		Error = error;
		Message = message;
	}

	public static ApiResult<T> Success(T data)
		=> new(true, data, null, null);

	public static ApiResult<T> Failure(string error, string message)
		=> new(false, default, error, message);
}
