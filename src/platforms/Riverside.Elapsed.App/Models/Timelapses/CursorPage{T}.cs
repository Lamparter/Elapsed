namespace Riverside.Elapsed.App.Models.Timelapses;

public class CursorPage<T> // infinite scroll
{
	public IReadOnlyList<T> Items;
	public string? NextCursor;
}
