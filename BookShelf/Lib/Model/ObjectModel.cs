using System.IO.Hashing;
using System.Text;

namespace BookShelf.Lib.Model;

public class ObjectModel
{
    public virtual string Protocol { get; init; } = default!;

    public virtual string Bucket { get; init; } = default!;

    public virtual string FullName { get; init; } = default!;

    public virtual ulong Size { get; init; } = default!;

    public virtual string Uri => $"{Protocol}://{Bucket}/{FullName}";

    public virtual string Name => Path.GetFileNameWithoutExtension(FullName);

    private string? _hash;

    public virtual string Hash => _hash ??= Convert.ToBase64String(XxHash128.Hash(Encoding.UTF8.GetBytes(Uri)), Base64FormattingOptions.None)
        .Replace('+', '-').Replace('/', '_').TrimEnd('=');
}
