using System;
using System.Collections.Generic;
using System.Text;

namespace Riverside.Elapsed.App.Models.Developer;

public class OAuthGrant
{
	public string GrantId;
	public string ServiceClientId;
	public string ServiceName;
	public IReadOnlyList<string> Scopes;
	public DateTimeOffset CreatedAt;
	public DateTimeOffset? LastUsedAt;
}
