namespace Riverside.Elapsed.Users;

public sealed class PrivateUserData
{
	public string PermissionLevel { get; set; } = default!;
	public List<Device> Devices { get; set; } = new();
	public bool NeedsReauth { get; set; }
}
