namespace TreeDataStructures;

public class AvlTreeNode<TKey, TValue>
    : BinaryTreeNode<TKey, TValue, AvlTreeNode<TKey, TValue>>
{
    public AvlTreeNode(TKey key, TValue value)
        : base(key, value)
    {
    }
}
