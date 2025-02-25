using BookShelf.Lib.Inject;

namespace BookShelf.Lib;

[Singleton]
public class Options
{
    public string? CredentialPath { get; init; }

    public string CacheLocation { get; init; } = default!;

    public List<ShelfLocation> ShelfLocations { get; init; } = [];

    public Options(IConfiguration configuration)
    {
        configuration.Bind(nameof(Options), this);
    }

    public class ShelfLocation
    {

        public string Name { get; set; } = default!;

        public string Location { get; set; } = default!;

    }
}
