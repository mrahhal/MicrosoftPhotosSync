namespace mps;

public enum ProcessedItemType
{
	Existed,
	Added,
	NotFound,
}

public class PatchedAlbum
{
	public PatchedAlbum(Album original)
	{
		Original = original;
	}

	public Album Original { get; }

	public string Name => Original.Album_Name;

	public int Id { get; set; }

	/// <summary>
	/// Whether this album is newly created or already existed.
	/// </summary>
	public bool Created => Id == 0;

	public int ItemsTotal { get; set; }

	public List<Item> ItemsExisted { get; set; } = new();

	public List<Item> ItemsAdded { get; set; } = new();

	public List<Item> ItemsNotFound { get; set; } = new();

	public Album ToNewAlbum()
	{
		return Original with
		{
			Album_Id = 0,
		};
	}
}
