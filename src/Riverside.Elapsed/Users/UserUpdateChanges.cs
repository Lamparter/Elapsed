namespace Riverside.Elapsed.Users;

public class UserUpdateChanges
{
	public string? Handle { get; set; }
	public string? DisplayName { get; set; }
	public string? Bio { get; set; }
	public List<string>? Urls { get; set; }
}
