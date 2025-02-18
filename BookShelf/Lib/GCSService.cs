using BookShelf.Lib.Inject;
using BookShelf.Lib.Model;
using BookShelf.Lib.Options;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using System.Collections.Concurrent;

namespace BookShelf.Lib;

[Singleton]
public class GCSService
{
    private readonly GCSOptions _options; // GCS の設定オプション

    private ConcurrentDictionary<string, Lazy<ValueTask<List<GCSObjectModel>>>> _entries = new();
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
    public async Task<List<GCSObjectModel>> FileListAsync(string bucket)
    {
        return await _entries.GetOrAdd(bucket, (bucket) =>
        {
            return new Lazy<ValueTask<List<GCSObjectModel>>>(() => Client.ListObjectsAsync(bucket).Select(obj => new GCSObjectModel()
            {
                Bucket = obj.Bucket,
                FullName = obj.Name,
                Size = obj.Size ?? 0
            }).ToListAsync());
        }).Value;
    }

    public async Task<GCSObjectModel?> GetCSObjectModel(string id)
    {
        foreach (var bucket in _options.ShelfBuckets)
        {
            var obj = (await FileListAsync(bucket.Bucket)).Find(x => x.Hash == id);
            if (obj != null)
            {
                return obj;
            }
        }
        return null;
    }

    public async Task DirectDownload(string bucket, string name, Stream writeableStream, DownloadObjectOptions? options = null)
    {
        await Client.DownloadObjectAsync(bucket, name, writeableStream, options);
    }

    public async Task DirectUpload(string bucket, string name, Stream readableStream)
    {
        await Client.UploadObjectAsync(bucket, name, "application/octet-stream", readableStream);
    }
}
