namespace TreeDataStructures;

public class SplayTreeNode<TKey, TValue>
    : BinaryTreeNode<TKey, TValue, SplayTreeNode<TKey, TValue>>
{
    public SplayTreeNode(TKey key, TValue value)
        : base(key, value)
    {
    }
}
