namespace Riverside.Elapsed.Users;

public sealed class UserUpdateRequest
{
	public string Id { get; set; } = default!;
	public UserUpdateChanges Changes { get; set; } = new();
}
