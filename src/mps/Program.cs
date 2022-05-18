using System.Text;
using mps;

Console.OutputEncoding = Encoding.UTF8;

var albumNames = new[]
{
	//"Czechia, September 2019", "Japan, November 2019", "France, December 2018",
	"可愛よ", "かわとっぷ",
}.ToHashSet();

var dry = false;

if (args.Any(a => a == "--dry"))
{
	dry = true;
	args = args.Where(a => a != "--dry").ToArray();
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

var patchedAlbums = new List<PatchedAlbum>();

foreach (var album in fromRepo.Albums.Where(x => albumNames.Contains(x.Album_Name))) // x.Album_State == 0
{
	var patched = new PatchedAlbum(album);
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
	Console.WriteLine();

	var createdText = album.Created ? "[New]" : "";
	Console.WriteLine($"- '{album.Name}' {createdText} ({album.ItemsTotal} items):");
	Console.WriteLine($"\tNew: {album.ItemsAdded.Count}\tExists: {album.ItemsExisted.Count}\tNot Found: {album.ItemsNotFound.Count}");
	if (album.ItemsNotFound.Any())
	{
		Console.WriteLine($"\tNot found:");

		foreach (var item in album.ItemsNotFound)
		{
			Console.WriteLine($"\t\t- {fromRepo.GetItemPath(item)}");
		}
	}

	// ---

	var id = album.Id;

	if (album.Created)
	{
		id = toRepo.CreateAlbum(album.ToNewAlbum());
	}

	var linkedItemsToCreate = album.ItemsAdded;
	foreach (var item in linkedItemsToCreate)
	{
		toRepo.CreateLink(new AlbumItemLink { AlbumItemLink_AlbumId = id, AlbumItemLink_ItemId = item.Item_Id });
	}

	if (!album.Created)
	{
		toRepo.UpdateAlbum(album);
	}
}
