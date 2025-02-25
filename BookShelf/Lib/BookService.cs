using BookShelf.Lib.Inject;
using SixLabors.ImageSharp;
using System.IO.Compression;
using System.IO.Hashing;
using System.Text;

namespace BookShelf.Lib;

[Singleton]
public class BookService
{
    private const int THUMB_WIDTH = (int)(148 * 2d);
    private const int THUMB_HEIGHT = (int)(210 * 2d);

    private readonly IDictionary<string, List<ObjectEntry>> _shelfCache;

    private readonly Options _options;

    public BookService(Options options)
    {
        _options = options;
        _shelfCache = Cache();
    }

    public BookEntry? GetEntry(string id)
    {
        return Get(id);
    }

    public Stream GetThumnail(string id)
    {
        var thumbData = new MemoryStream();
        var location = _options.CacheLocation;
        var path = Path.Combine(location, $"{id}.thumb.png");
        if (File.Exists(path))
        {
            return new FileStream(path, FileMode.Open, FileAccess.Read);
        }
        else
        {
            var entry = Get(id);
            if (entry == null)
            {
                return new MemoryStream();
            }
            var coverFileName = (entry.Files.Find(e => e.Name.StartsWith("cover", StringComparison.InvariantCultureIgnoreCase)) ?? entry.Files.First()).Name;
            using var archive = new ZipArchive(new FileStream(entry.FullName, FileMode.Open, FileAccess.Read));
            using var stream = archive.GetEntry(coverFileName)!.Open();
            Image.Load(stream).Resize(THUMB_WIDTH, THUMB_HEIGHT).SaveAsPng(thumbData);
            thumbData.Position = 0;
        }
        thumbData.Position = 0;
        return thumbData;
    }

    public Stream GetPage(string id, string page)
    {
        var entry = Get(id);
        if (entry == null)
        {
            return new MemoryStream();
        }
        using var archive = new ZipArchive(new FileStream(entry.FullName, FileMode.Open, FileAccess.Read));
        var zipEntry = archive.GetEntry(page);
        if (zipEntry == null)
        {
            return new MemoryStream();
        }
        using var stream = zipEntry.Open();
        var copy = new MemoryStream();
        stream.CopyTo(copy);
        copy.Position = 0;
        return copy;
    }

    public List<ObjectEntry> GetObjects(string shelfName)
    {
        if(_shelfCache.TryGetValue(shelfName, out List<ObjectEntry>? objects))
        {
            return objects;
        }
        return [];
    }

    private Dictionary<string, List<ObjectEntry>> Cache()
    {
        var cache = new Dictionary<string, List<ObjectEntry>>();
        foreach (var location in _options.ShelfLocations)
        {
            cache.Add(location.Name, Directory.EnumerateFiles(location.Location, "*", SearchOption.AllDirectories)
                .Where(path => path.EndsWith(".zip") || path.EndsWith(".cbz"))
                .Select(path => new ObjectEntry(path))
                .ToList());
        }
        return cache;
    }

    private BookEntry? Get(string id)
    {
        var obj = _shelfCache.Values.SelectMany(val => val)
            .FirstOrDefault(obj => obj.Id == id);
        if (obj == null)
        {
            return null;
        }
        using var archive = new ZipArchive(new FileStream(obj.FullName, FileMode.Open, FileAccess.Read));
        return new BookEntry(id, obj.FullName, archive.Entries
            .OrderBy(e => e.Name)
            .Where(e => e.Name.EndsWith(".jpg", StringComparison.InvariantCultureIgnoreCase) || e.Name.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase) || e.Name.EndsWith(".webp", StringComparison.InvariantCultureIgnoreCase))
            .Select(e => new FileEntry(e.Name, e.Length))
            .ToList());
    }

    public record BookEntry(string Id, string FullName, List<FileEntry> Files)
    {
        public string Name => Path.GetFileNameWithoutExtension(FullName);
    }

    public record FileEntry(string Name, long Size);

    public record ObjectEntry(string FullName)
    {
        private string? _hash;

        public string Name => Path.GetFileNameWithoutExtension(FullName);

        public virtual string Id => _hash ??= Convert.ToBase64String(XxHash64.Hash(Encoding.UTF8.GetBytes(FullName)), Base64FormattingOptions.None)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
