namespace TreeDataStructures;

public class TreapNode<TKey, TValue>
    : BinaryTreeNode<TKey, TValue, TreapNode<TKey, TValue>>
{
    public TreapNode(TKey key, TValue value, int priority)
        : base(key, value)
    {
        Priority = priority;
    }

    public int Priority { get; }
}
