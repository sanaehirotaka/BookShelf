using Google.Cloud.Storage.V1;
using Object = Google.Apis.Storage.v1.Data.Object;

namespace BookShelf.Lib;

public class GCSStreamSource
{
    public const int MIN_CHUNK_SIZE = 1024 * 64;
    public const int CHUNK_SIZE = 1024 * 1024;
    private readonly LRUCache<int, byte[]> _rangeCache = new();
    private readonly StorageClient _client;
    private readonly Object _obj;
    private readonly int _chunkSize;

    public GCSStreamSource(StorageClient client, string bucket, string name, int chunkSize = CHUNK_SIZE)
    {
        _client = client;
        _obj = client.GetObject(bucket, name);
        _chunkSize = Math.Max(chunkSize, MIN_CHUNK_SIZE);
    }

    public Stream NewStream()
    {
        return new GCSRangeStream(_rangeCache, _client, _obj, _chunkSize);
    }

    private class GCSRangeStream : Stream
    {
        private readonly LRUCache<int, byte[]> rangeCache;
        private readonly StorageClient _client;
        private readonly Object _obj;
        private readonly int _chunkSize;

        private long? _len;
        private long _position = 0;

        public GCSRangeStream(LRUCache<int, byte[]> rangeCache, StorageClient client, Object obj, int chunkSize)
        {
            this.rangeCache = rangeCache;
            _client = client;
            _obj = obj;
            _chunkSize = chunkSize;
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => _len ??= (long)(_obj.Size ?? 0);
        public override long Position { get => _position; set => _position = value; }

        public override int Read(byte[] buffer, int offset, int length)
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
                int chunkOffset = i == startChunk ? (int)(_position % _chunkSize) : 0;
                int bytesToCopy = Math.Min(chunk.Length - chunkOffset, length - totalBytesRead);
                Array.Copy(chunk, chunkOffset, buffer, offset + totalBytesRead, bytesToCopy);
                totalBytesRead += bytesToCopy;
            }
            _position += totalBytesRead;
            return totalBytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
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

        private byte[] RangeRequest(long start, int length)
        {
            long end = Math.Min(start + length - 1, Length - 1);
            if (start > end)
            {
                // 空の配列を返す
                return Array.Empty<byte>();
            }
            using var stream = new MemoryStream();
            _client.DownloadObject(_obj.Bucket, _obj.Name, stream, new()
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
}
