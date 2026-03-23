using System;
using System.Collections.Generic;
using System.Text;

namespace Riverside.Elapsed.App.Models.Timelapses;

public class DraftTimelapse
{
	public string DraftTimelapseId;
	public string Name;
	public string Description;
	public DateTimeOffset CreatedAt;
	public User.User Owner;
	public Guid DeviceId;
	public byte[] IvHex;
	public Uri PreviewThumbnailUrl;
	public IReadOnlyList<Uri> Sessions;
	public IReadOnlyList<DraftEdit> EditList;
	public string? AssociatedTimelapseId;
}
