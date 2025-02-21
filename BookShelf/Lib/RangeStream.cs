using Google.Cloud.Storage.V1;
using System.Collections.Concurrent;
using Object = Google.Apis.Storage.v1.Data.Object;

namespace BookShelf.Lib;

public partial class RangeStream : Stream
{
    private const int MIN_CHUNK_SIZE = 1024 * 64;
    private const int CHUNK_SIZE = 1024 * 1024;
    private readonly StorageClient _client;
    private readonly int _chunkSize;
    private readonly string _bucket;
    private readonly string _name;
    private readonly LRUCache<int, byte[]> rangeCache = new();

    private Object? _obj;
    private long? _len;
    private long _position = 0;

    public RangeStream(StorageClient client, string bucket, string name, int chunkSize = CHUNK_SIZE)
    {
        _client = client;
        _bucket = bucket;
        _name = name;
        _chunkSize = Math.Max(chunkSize, MIN_CHUNK_SIZE);
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => _len ??= (long)(Object.Size ?? 0);
    public override long Position
    {
        get {
            return _position;
        }
        set
        {
            lock (this)
            {
                _position = value;
            }
        }
    }
    private Object Object => _obj ??= _client.GetObject(_bucket, _name);

    public override int Read(byte[] buffer, int offset, int length)
    {
        lock(this)
        {
            if (length <= 0 || _position >= Length)
            {
                return 0;
            }
            if (_position >= Length)
            {
                return 0;
            }
            length = (int)Math.Min(length, Length - _position);

            int startChunk = (int)(_position / _chunkSize);
            int endChunk = (int)((_position + length - 1) / _chunkSize);
            int totalBytesRead = 0;
            for (int i = startChunk; i <= endChunk; i++)
            {
                byte[] chunk = rangeCache.GetOrAdd(i, chunkIndex => RangeRequest(chunkIndex * _chunkSize, _chunkSize));
                int chunkOffset = (i == startChunk) ? (int)(_position % _chunkSize) : 0;
                int bytesToCopy = Math.Min(chunk.Length - chunkOffset, length - totalBytesRead);
                Array.Copy(chunk, chunkOffset, buffer, offset + totalBytesRead, bytesToCopy);
                totalBytesRead += bytesToCopy;
            }
            _position += totalBytesRead;
            return totalBytesRead;
        }
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        lock(this)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    _position = offset;
                    break;
                case SeekOrigin.Current:
                    _position += offset;
                    break;
                case SeekOrigin.End:
                    _position = Length + offset;
                    break;
            }
            // 範囲外チェック
            _position = Math.Max(0, Math.Min(_position, Length));
            return _position;
        }
    }

    private byte[] RangeRequest(long start, int length)
    {
        long end = Math.Min(start + length - 1, Length - 1);
        if (start > end)
        {
            // 空の配列を返す
            return Array.Empty<byte>();
        }
        using var stream = new MemoryStream();
        _client.DownloadObject(_bucket, _name, stream, new()
        {
            ChunkSize = Math.Min(_chunkSize, CHUNK_SIZE),
            Range = new(start, end)
        });
        return stream.ToArray();
    }

    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Flush() => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
