namespace TreeDataStructures;

public class BinarySearchTree<TKey, TValue>
    : BinarySearchTreeBase<TKey, TValue, BinarySearchTreeNode<TKey, TValue>>
{
    public BinarySearchTree(IComparer<TKey>? comparer = null)
        : base(comparer)
    {
    }

    protected override BinarySearchTreeNode<TKey, TValue> CreateNode(TKey key, TValue value)
    {
        return new BinarySearchTreeNode<TKey, TValue>(key, value);
    }
}
