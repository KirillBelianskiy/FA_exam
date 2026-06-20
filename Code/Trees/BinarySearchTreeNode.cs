namespace TreeDataStructures;

public class BinarySearchTreeNode<TKey, TValue>
    : BinaryTreeNode<TKey, TValue, BinarySearchTreeNode<TKey, TValue>>
{
    public BinarySearchTreeNode(TKey key, TValue value)
        : base(key, value)
    {
    }
}
