using BookShelf.Lib.Inject;
using BookShelf.Lib.Model;
using BookShelf.Lib.Options;
using SixLabors.ImageSharp;
using System.IO.Compression;

namespace BookShelf.Lib;

[Singleton]
public class BookService
{
    private const int THUMB_WIDTH = (int)(148 * 2d);
    private const int THUMB_HEIGHT = (int)(210 * 2d);

    private readonly GCSOptions _options;
    private readonly GCSService _storageService;

    private LRUCache<string, ContainerEntry> _cache = new();

    public BookService(GCSOptions options, GCSService storageService)
    {
        _options = options;
        _storageService = storageService;
    }

    public BookEntry GetEntry(string id)
    {
        var entry = Get(id);
        lock (entry)
        {
            return new(id, entry.Model.Name, GetFileEntries(entry).Select(e => new FileEntry(e.Name, e.Length)).ToList());
        }
    }

    public async Task<Stream> GetThumnail(string id)
    {
        var thumbData = new MemoryStream();
        var bucket = _options.CacheBucket;
        var name = $"{id}.thumb.png";
        if (_storageService.HasExistObject(bucket, name))
        {
            await _storageService.DownloadAsync(bucket, name, thumbData);
        }
        else
        {
            var entry = Get(id);
            lock (entry)
            {
                var entries = GetFileEntries(entry);
                var coverEntry = entries.Find(e => e.Name.StartsWith("cover", StringComparison.InvariantCultureIgnoreCase)) ?? entries.First();
                Image.Load(coverEntry.Open()).Resize(THUMB_WIDTH, THUMB_HEIGHT).SaveAsPng(thumbData);
            }
            thumbData.Position = 0;
            await _storageService.UploadAsync(bucket, name, thumbData);
        }
        thumbData.Position = 0;
        return thumbData;
    }

    public Stream GetPage(string id, string page)
    {
        var entry = Get(id);
        lock(entry)
        {
            using var stream = GetFileEntries(entry)
                .Find(e => e.Name.Equals(page, StringComparison.InvariantCultureIgnoreCase))?.Open() ?? new MemoryStream();
            var copy = new MemoryStream();
            stream.CopyTo(copy);
            copy.Position = 0;
            return copy;
        }
    }

    private List<ZipArchiveEntry> GetFileEntries(ContainerEntry entry)
    {
        return entry.Archive.Entries.OrderBy(e => e.Name)
            .Where(e => e.Name.EndsWith(".jpg", StringComparison.InvariantCultureIgnoreCase) || e.Name.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase) || e.Name.EndsWith(".webp", StringComparison.InvariantCultureIgnoreCase))
            .ToList();
    }

    private ContainerEntry Get(string id)
    {
        return _cache.GetOrAdd(id, id =>
        {
            var obj = _storageService.GetCSObjectModel(id) ?? throw new InvalidOperationException(id);
            var archive = new ZipArchive(new RangeStream(_storageService.Client, obj.Bucket, obj.FullName, 1024 * 128));
            return new ContainerEntry(obj, archive);
        });
    }

    public record BookEntry(string Id, string Name, IList<FileEntry> Files);

    public record FileEntry(string Name, long Size);

    private record ContainerEntry(GCSObjectModel Model, ZipArchive Archive);
}
