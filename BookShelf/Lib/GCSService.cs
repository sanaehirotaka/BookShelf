using BookShelf.Lib.Inject;
using BookShelf.Lib.Model;
using BookShelf.Lib.Options;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using System.Collections.Concurrent;
using Object = Google.Apis.Storage.v1.Data.Object;

namespace BookShelf.Lib;

[Scoped]
public class GCSService
{
    private readonly GCSOptions _options; // GCS の設定オプション

    private ConcurrentDictionary<(string Bucket, string Name), Object?> _cache = new();

    private ConcurrentDictionary<string, List<GCSObjectModel>> _entries = new();

    public StorageClient Client { get; set; } // GCS クライアント

    public GCSService(GCSOptions options)
    {
        _options = options;
        if (string.IsNullOrEmpty(options.CredentialPath))
        {
            Client = StorageClient.Create(GoogleCredential.GetApplicationDefault());
        }
        else
        {
            Client = StorageClient.Create(GoogleCredential.FromFile(options.CredentialPath));
        }
    }

    /// <summary>
    /// GCSバケット内のファイルオブジェクトを非同期的に列挙する。
    /// </summary>
    public List<GCSObjectModel> FileList(string bucket)
    {
        return _entries.GetOrAdd(bucket, (bucket) =>
        {
            return ListObjects(bucket).Select(obj => new GCSObjectModel()
            {
                Bucket = obj.Bucket,
                FullName = obj.Name,
                Size = obj.Size ?? 0
            }).ToList();
        });
    }

    public GCSObjectModel? GetCSObjectModel(string id)
    {
        foreach (var bucket in _options.ShelfBuckets)
        {
            var obj = FileList(bucket.Bucket).Find(x => x.Hash == id);
            if (obj != null)
            {
                return obj;
            }
        }
        return null;
    }

    public bool HasExistObject(string bucket, string name)
    {
        return GetObject(bucket, name) is not null;
    }

    public async Task DownloadAsync(string bucket, string name, Stream writeableStream, DownloadObjectOptions? options = null)
    {
        var obj = await Client.DownloadObjectAsync(bucket, name, writeableStream, options);
        _cache.AddOrUpdate((bucket, name), key => obj, (key, old) => obj);
    }

    public async Task UploadAsync(string bucket, string name, Stream readableStream)
    {
        var obj = await Client.UploadObjectAsync(bucket, name, "application/octet-stream", readableStream);
        _cache.AddOrUpdate((bucket, name), key => obj, (key, old) => obj);
    }

    // private

    private List<Object> ListObjects(string bucket)
    {
        var list = Client.ListObjects(bucket)
            .Where(o => o.Size != null)
            .ToList();
        list.ForEach(obj => _cache.AddOrUpdate((obj.Bucket, obj.Name), key => obj, (key, old) => obj));
        return list;
    }

    private Object? GetObject(string bucket, string name)
    {
        return _cache.GetOrAdd((bucket, name), key =>
        {
            try
            {
                return Client.GetObject(bucket, name);
            }
            catch
            {
                return null;
            }
        });
    }

}
