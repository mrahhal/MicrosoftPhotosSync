using System.Diagnostics;
using Dapper;

namespace mps;

public class Folder
{
	public int Folder_Id { get; set; }
	public int Folder_ParentFolderId { get; set; }
	public string Folder_Path { get; set; } = null!;
	public string Folder_DisplayName { get; set; } = null!;
}

public class Item
{
	public int Item_Id { get; set; }
	public int Item_ParentFolderId { get; set; }
	public string Item_FileName { get; set; } = null!;
	public string Item_FileExtension { get; set; } = null!;
}

[DebuggerDisplay("{Album_Name}: {Album_Type} ; {Album_State}")]
public record class Album
{
	[Key]
	public int Album_Id { get; set; }
	public string Album_Name { get; set; } = null!;
	public int Album_Type { get; set; }
	public int Album_State { get; set; }
	public long? Album_DateCreated { get; set; }
	public long? Album_DateUpdated { get; set; }
	public long? Album_DateUserModified { get; set; }
	public long? Album_DateViewed { get; set; }
	public long? Album_Count { get; set; }
}

public class AlbumItemLink
{
	public int AlbumItemLink_AlbumId { get; set; }
	public int AlbumItemLink_ItemId { get; set; }

	public (int, int) Id => (AlbumItemLink_AlbumId, AlbumItemLink_ItemId);
}
