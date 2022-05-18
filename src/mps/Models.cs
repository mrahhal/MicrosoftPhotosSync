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
	public int Album_QueryType { get; set; }
	public int Album_QueryBoundsType { get; set; }
	public string Album_Query { get; set; } = null!;
	public long? Album_DateCreated { get; set; }
	public long? Album_DateUpdated { get; set; }
	public long? Album_DateUserModified { get; set; }
	public long? Album_DateViewed { get; set; }
	public long? Album_DateShared { get; set; }
	public long? Album_Count { get; set; }
	public double Album_CoverBoundsLeft { get; set; }
	public double Album_CoverBoundsTop { get; set; }
	public double Album_CoverBoundsRight { get; set; }
	public double Album_CoverBoundsBottom { get; set; }
	public int Album_Visibility { get; set; }
	public long? Album_EventStartDate { get; set; }
	public long? Album_EventEndDate { get; set; }
	public long? Album_SummaryStartDate { get; set; }
	public long? Album_SummaryEndDate { get; set; }
	public int Album_Source { get; set; }
	public int Album_SourceId { get; set; }
	public int Album_PublishState { get; set; }
	public int Album_PendingTelemetryUploadState { get; set; }
	public int Album_SentTelemetryUploadState { get; set; }
	public string Album_ETag { get; set; } = null!;
	public int Album_CreationType { get; set; }
	public int Album_Order { get; set; }
}

public class AlbumItemLink
{
	public int AlbumItemLink_AlbumId { get; set; }
	public int AlbumItemLink_ItemId { get; set; }

	public (int, int) Id => (AlbumItemLink_AlbumId, AlbumItemLink_ItemId);
}
