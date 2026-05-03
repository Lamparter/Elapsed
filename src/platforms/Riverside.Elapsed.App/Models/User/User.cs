using System;
using System.Collections.Generic;
using System.Text;

namespace Riverside.Elapsed.App.Models.User;

public class User
{
	public string UserId;
	public DateTimeOffset CreatedAt;
	public string Handle;
	public string DisplayName;
	public Uri ProfilePictureUrl;
	public string Bio;
	public IReadOnlyList<Uri> Urls;
	public string? HackatimeId;
	public string? SlackId;
}
