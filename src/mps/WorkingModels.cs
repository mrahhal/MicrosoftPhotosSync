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
		Inner = original;
	}

	public Album Inner { get; }

	public string Name => Inner.Album_Name;

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
		return Inner with
		{
			Album_Id = 0,
		};
	}
}

// A path that can be either a local full path or a remote folder id + a local filename.
public record ResilientPath(
	string? Local,
	ResilientPathRemote? Remote
)
{
	public static ResilientPath CreateLocal(string local) => new (local.ToLower(), null);
	public static ResilientPath CreateRemote(ResilientPathRemote remote) => new (null, remote);
}

public record ResilientPathRemote(
	int FolderId,
	string FileName
);
