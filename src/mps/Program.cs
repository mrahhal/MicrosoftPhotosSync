using System.Text;
using mps;

Console.OutputEncoding = Encoding.UTF8;

var dry = false;
var indent = "    ";

if (args.Any(a => a == "--dry"))
{
	dry = true;
	args = args.Where(a => a != "--dry").ToArray();
}

if (dry)
{
	ColorConsole.WriteInfo("dry");
	Console.WriteLine("===");
	Console.WriteLine();
}

if (args.Length != 2)
{
	Console.WriteLine(
@"Syncs albums from one Microsoft Photos db to another.

Usage:
mps [from] [to]

--dry: Dry run");
	Environment.Exit(1);
}

var from = args[0];
var to = args[1];

using var fromRepo = Repository.Load(from);
using var toRepo = Repository.Load(to);

var oldItemIdToNewItemId = new Dictionary<int, int>();
foreach (var fromItem in fromRepo.Items)
{
	var p = fromRepo.GetItemPath(fromItem);
	if (toRepo.PathToItemMap.TryGetValue(p, out var toItem))
	{
		oldItemIdToNewItemId[fromItem.Item_Id] = toItem.Item_Id;
	}
}

var patchedAlbums = new List<PatchedAlbum>();

int? GetNewItemIdFromOldItemId(int? oldItemId)
{
	if (oldItemId == null) return null;
	if (oldItemIdToNewItemId.TryGetValue(oldItemId.Value, out var newItemId))
	{
		return newItemId;
	}
	return null;
}

foreach (var album in fromRepo.Albums.Where(x => x.Album_Visibility == 1))
{
	var patched = new PatchedAlbum(album with
	{
		Album_CoverItemId = GetNewItemIdFromOldItemId(album.Album_CoverItemId),
	});
	patchedAlbums.Add(patched);

	var existingAlbum = toRepo.Albums.FirstOrDefault(toAlbum => toAlbum.Album_Name == album.Album_Name);
	if (existingAlbum != null)
	{
		patched.Id = existingAlbum.Album_Id;
	}

	var itemsInAlbum = fromRepo.GetItemsInAlbum(album);
	patched.ItemsTotal = itemsInAlbum.Count;

	var items = itemsInAlbum.Select(fromItem =>
	{
		var existingItem = toRepo.Items.FirstOrDefault(toItem => ItemsEqual(fromItem, toItem));
		if (existingItem == null)
		{
			return (ProcessedItemType.NotFound, fromItem);
		}

		if (existingAlbum != null && toRepo.ContainsAlbumItemLink(existingAlbum.Album_Id, existingItem.Item_Id))
		{
			return (ProcessedItemType.Existed, existingItem);
		}

		return (ProcessedItemType.Added, existingItem);
	}).ToList();

	patched.ItemsExisted = items.Where(x => x.Item1 == ProcessedItemType.Existed).Select(x => x.Item2).ToList()!;
	patched.ItemsAdded = items.Where(x => x.Item1 == ProcessedItemType.Added).Select(x => x.Item2).ToList()!;
	patched.ItemsNotFound = items.Where(x => x.Item1 == ProcessedItemType.NotFound).Select(x => x.Item2).ToList()!;

	ProcessPatched(patched);
}

bool ItemsEqual(Item fromItem, Item toItem)
{
	return fromRepo.GetItemPath(fromItem) == toRepo.GetItemPath(toItem);
}

void ProcessPatched(PatchedAlbum album)
{
	var createdText = album.Created ? "[Green](New)[/Green]" : "";
	ColorConsole.WriteEmbeddedColorLine($"Album: [Info]'{album.Name}'[/Info] {createdText} ({album.ItemsTotal} items)");
	Console.WriteLine();
	if (!album.ItemsAdded.Any())
	{
		if (!album.ItemsNotFound.Any())
		{
			ColorConsole.WriteSuccess("Fully synced");
		}
		else
		{
			ColorConsole.WriteSuccess("Partially synced");
		}
	}
	if (album.ItemsAdded.Any())
	{
		ColorConsole.WriteSuccess($"{indent}{album.ItemsAdded.Count} found and added");
	}
	if (album.ItemsNotFound.Any())
	{
		ColorConsole.WriteError($"{indent}{album.ItemsNotFound.Count} not found:");
		foreach (var item in album.ItemsNotFound)
		{
			ColorConsole.WriteError($"{indent}{indent}- {fromRepo.GetItemPath(item)}");
		}
	}

	Console.WriteLine();
	Console.WriteLine("================");
	Console.WriteLine();

	// ---

	var id = album.Id;

	if (album.Created)
	{
		id = toRepo.CreateAlbum(album.ToNewAlbum());
	}
	else
	{
		toRepo.UpdateAlbum(album);
	}

	var linkedItemsToCreate = album.ItemsAdded;
	foreach (var item in linkedItemsToCreate)
	{
		toRepo.CreateLink(new AlbumItemLink { AlbumItemLink_AlbumId = id, AlbumItemLink_ItemId = item.Item_Id });
	}
}

if (dry)
{
	toRepo.Rollback();
}
else
{
	toRepo.Commit();
}
