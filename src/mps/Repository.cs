﻿using System.Collections.Concurrent;
using Dapper;
using Microsoft.Data.Sqlite;

namespace mps;

public sealed class Repository : IDisposable
{
	private readonly SqliteConnection _c;
	private readonly SqliteTransaction _t;

	public Repository(SqliteConnection c)
	{
		_c = c;
		_t = _c.BeginTransaction();
	}

	public List<Folder> Folders { get; private set; } = null!;
	public Dictionary<int, Folder> FoldersById { get; private set; } = null!;

	public List<Item> Items { get; private set; } = null!;
	public Dictionary<int, Item> ItemsById { get; private set; } = null!;

	public List<Album> Albums { get; private set; } = null!;
	public Dictionary<int, Album> AlbumsById { get; private set; } = null!;

	public List<AlbumItemLink> AlbumItemLinks { get; private set; } = null!;
	private HashSet<(int, int)> AlbumItemLinksSet { get; set; } = null!;
	public Dictionary<int, List<AlbumItemLink>> AlbumItemLinksByAlbumId { get; private set; } = null!;
	public Dictionary<int, List<AlbumItemLink>> AlbumItemLinksByItemId { get; private set; } = null!;

	public Dictionary<ResilientPath, Item> PathToItemMap { get; private set; } = new();

	private ConcurrentDictionary<Album, List<Item>> ItemsInAlbumCache { get; } = new();

	public static Repository Load(string dbPath)
	{
		var c = new SqliteConnection($"Data Source={dbPath}");
		c.CreateCollation("NoCaseUnicode", (s1, s2) => string.Compare(s1, s2, StringComparison.OrdinalIgnoreCase));
		c.Open();
		var r = new Repository(c);
		r.Load();
		return r;
	}

	public void Dispose()
	{
		_t.Dispose();
		_c.Dispose();
	}

	public void Commit()
	{
		_t.Commit();
	}

	public void Rollback()
	{
		_t.Rollback();
	}

	void Load()
	{
		Folders = SelectFolders();
		FoldersById = Folders.ToDictionary(x => x.Folder_Id);

		Items = SelectItems();
		ItemsById = Items.ToDictionary(x => x.Item_Id);

		Albums = SelectAlbums();
		AlbumsById = Albums.ToDictionary(x => x.Album_Id);

		AlbumItemLinks = SelectAlbumItemLinks();
		AlbumItemLinksSet = AlbumItemLinks.Select(x => (x.AlbumItemLink_AlbumId, x.AlbumItemLink_ItemId)).ToHashSet();
		AlbumItemLinksByAlbumId = AlbumItemLinks.GroupBy(x => x.AlbumItemLink_AlbumId).ToDictionary(x => x.Key, x => x.ToList());
		AlbumItemLinksByItemId = AlbumItemLinks.GroupBy(x => x.AlbumItemLink_ItemId).ToDictionary(x => x.Key, x => x.ToList());

		PathToItemMap = Items.ToDictionary(x =>
		{
			return GetItemPath(x);
		});
	}

	public bool ContainsAlbumItemLink(int albumId, int itemId)
	{
		return AlbumItemLinksSet.Contains((albumId, itemId));
	}

	public List<Item> GetItemsInAlbum(Album album)
	{
		return ItemsInAlbumCache.GetOrAdd(album, album =>
		{
			var itemsInAlbum = Items.Where(item =>
			{
				if (AlbumItemLinksByItemId.TryGetValue(item.Item_Id, out var links))
				{
					return links.Any(link => link.AlbumItemLink_AlbumId == album.Album_Id);
				}
				return false;
			});

			return itemsInAlbum.ToList();
		});
	}

	public ResilientPath GetItemPath(Item item)
	{
		var folder = FoldersById[item.Item_ParentFolderId];
		if (folder.Folder_Path != null)
		{
			var fullPath = $"{folder.Folder_Path}\\{item.Item_FileName}";
			return ResilientPath.CreateLocal(fullPath);
		}
		else
		{
			return ResilientPath.CreateRemote(new(folder.Folder_Id, item.Item_FileName));
		}
	}

	public int CreateAlbum(Album album)
	{
		return _c.Insert(album)!.Value;
	}

	public void UpdateAlbum(PatchedAlbum patchedAlbum)
	{
		var count = patchedAlbum.Inner.Album_Count + patchedAlbum.ItemsAdded.Count;
		_c.Execute(@$"
UPDATE {TableNames.Album} SET Album_Count = @count, Album_CoverItemId = @coverItemId WHERE Album_Id = @id",
			new { id = patchedAlbum.Id, count, coverItemId = patchedAlbum.Inner.Album_CoverItemId });
	}

	public void CreateLink(AlbumItemLink link)
	{
		_c.Execute(@$"
INSERT INTO {TableNames.AlbumItemLink} (AlbumItemLink_AlbumId, AlbumItemLink_ItemId, AlbumItemLink_Order, AlbumItemLink_ItemPhotosCloudId) VALUES (@AlbumItemLink_AlbumId, @AlbumItemLink_ItemId, @AlbumItemLink_Order, @AlbumItemLink_ItemPhotosCloudId)",
			link);
	}

	List<Folder> SelectFolders()
	{
		return _c.Query<Folder>(@$"
SELECT * FROM {TableNames.Folder}")
			.ToList();
	}

	List<Item> SelectItems()
	{
		return _c.Query<Item>(@$"
SELECT * FROM {TableNames.Item}")
			.ToList();
	}

	List<Album> SelectAlbums()
	{
		var list = _c.Query<Album>(@$"
SELECT * FROM {TableNames.Album}")
			.ToList();

		foreach (var album in list)
		{
			album.Album_Name = album.Album_Name.Replace("‎", "");
		}

		return list;
	}

	List<AlbumItemLink> SelectAlbumItemLinks()
	{
		return _c.Query<AlbumItemLink>(@$"
SELECT * FROM {TableNames.AlbumItemLink}")
			.ToList();
	}
}
