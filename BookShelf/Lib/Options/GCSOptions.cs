using BookShelf.Lib.Inject;

namespace BookShelf.Lib.Options;

[Singleton]
public class GCSOptions
{
    public string? CredentialPath { get; init; }

    public string CacheBucket { get; init; } = default!;

    public List<ShelfBucket> ShelfBuckets { get; init; } = [];

    public GCSOptions(IConfiguration configuration)
    {
        configuration.Bind(nameof(GCSOptions), this);
    }

    public class ShelfBucket {

        public string Name { get; set; } = default!;

        public string Bucket { get; set; } = default!;

    }
}
