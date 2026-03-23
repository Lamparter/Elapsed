using System;
using System.Collections.Generic;
using System.Text;

namespace Riverside.Elapsed.App.Models.User;

public sealed class Myself : User
{
	public IReadOnlyList<Device> Devices;
	public bool NeedsReauth;
	public PermissionLevel PermissionLevel;
}
