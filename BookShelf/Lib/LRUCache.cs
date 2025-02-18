namespace BookShelf.Lib;

public class LRUCache<TKey, TValue> where TKey : IEquatable<TKey>
{
    private readonly int _capacity;
    private readonly Dictionary<TKey, LinkedListNode<(TKey Key, TValue Value)>> _cache;
    private readonly LinkedList<(TKey Key, TValue Value)> _lruList;

    public LRUCache(int capacity = 10)
    {
        _capacity = capacity;
        _cache = new Dictionary<TKey, LinkedListNode<(TKey, TValue)>>(capacity);
        _lruList = new LinkedList<(TKey, TValue)>();
    }

    public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
    {
        if (_cache.TryGetValue(key, out var node))
        {
            // 既存の要素をリストの先頭に移動 (最近使用されたことを示す)
            _lruList.Remove(node);
            _lruList.AddFirst(node);
            return node.Value.Value;
        }
        else
        {
            // キーが存在しない場合、ファクトリ関数を使用して値を生成
            TValue newValue = valueFactory(key);

            // 新しいキーの場合 (Put メソッドのロジックを再利用)
            if (_cache.Count >= _capacity)
            {
                // キャッシュがいっぱいの場合、最も古い要素を削除
                var lastNode = _lruList.Last;
                _lruList.RemoveLast();
                _cache.Remove(lastNode!.Value.Key);
            }
            // 新しい要素をリストと辞書に追加
            var newNode = new LinkedListNode<(TKey, TValue)>((key, newValue));
            _lruList.AddFirst(newNode);
            _cache[key] = newNode;

            return newValue;
        }
    }

    public int Count => _cache.Count; //added count property
}
