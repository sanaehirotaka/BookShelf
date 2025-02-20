using BookShelf.Lib.Inject;
using BookShelf.Lib.Model;
using BookShelf.Lib.Options;
using SixLabors.ImageSharp;
using System.IO.Compression;

namespace BookShelf.Lib;

[Singleton]
public class BookService
{
    private const int THUMB_WIDTH = (int)(148 * 1.5d);
    private const int THUMB_HEIGHT = (int)(210 * 1.5d);

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
        return new(id, Get(id).Model.Name, GetFileEntries(id).Select(e => new FileEntry(e.Name, e.Length)).ToList());
    }

    public async Task<Stream> GetThumnail(string id)
    {
        var thumbData = new MemoryStream();

        var bucket = _options.CacheBucket;
        var name = $"{id}.thumb.jpg";

        if (_storageService.HasExistObject(bucket, name))
        {
            await _storageService.DownloadAsync(bucket, name, thumbData);
        }
        else
        {
            var entries = GetFileEntries(id);
            var coverEntry = entries.Find(e => e.Name.StartsWith("cover", StringComparison.InvariantCultureIgnoreCase)) ?? entries.First();
            Image.Load(coverEntry.Open()).Resize(THUMB_WIDTH, THUMB_HEIGHT).SaveAsJpeg(thumbData);
            thumbData.Position = 0;
            await _storageService.UploadAsync(bucket, name, thumbData);
        }
        thumbData.Position = 0;
        return thumbData;
    }

    public Stream GetPage(string id, string page)
    {
        return GetFileEntries(id).Find(e => e.Name.Equals(page, StringComparison.InvariantCultureIgnoreCase))?.Open() ?? new MemoryStream();
    }

    private List<ZipArchiveEntry> GetFileEntries(string id)
    {
        return Get(id).Archive.Entries.OrderBy(e => e.Name)
            .Where(e => e.Name.EndsWith(".jpg", StringComparison.InvariantCultureIgnoreCase) || e.Name.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase) || e.Name.EndsWith(".webp", StringComparison.InvariantCultureIgnoreCase))
            .ToList();
    }

    private ContainerEntry Get(string id)
    {
        return _cache.GetOrAdd(id, id =>
        {
            var obj = _storageService.GetCSObjectModel(id) ?? throw new InvalidOperationException(id);
            var archive = new ZipArchive(new RangeStream(_storageService.Client, obj.Bucket, obj.FullName, 1024 * 512));
            return new ContainerEntry(obj, archive);
        });
    }

    public record BookEntry(string Id, string Name, IList<FileEntry> Files);

    public record FileEntry(string Name, long Size);

    private record ContainerEntry(GCSObjectModel Model, ZipArchive Archive);
}
