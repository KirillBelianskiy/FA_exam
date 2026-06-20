namespace TreeDataStructures;

public readonly struct TreeEntry<TKey, TValue>
{
    public TreeEntry(TKey key, TValue value, int height)
    {
        Key = key;
        Value = value;
        Height = height;
    }

    public TKey Key { get; }

    public TValue Value { get; }

    public int Height { get; }
}
