using System.Text.Json;

namespace Riverside.Elapsed.App.Models.Admin;

public class AdminListPage
{
	public EntityType Entity;
	public IReadOnlyList<JsonElement> Rows;
	public long Total;
	public long Page;
	public long PageSize;
}
