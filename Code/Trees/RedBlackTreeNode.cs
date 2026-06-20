namespace TreeDataStructures;

public class RedBlackTreeNode<TKey, TValue>
    : BinaryTreeNode<TKey, TValue, RedBlackTreeNode<TKey, TValue>>
{
    public RedBlackTreeNode(TKey key, TValue value)
        : base(key, value)
    {
        Color = RedBlackColor.Red;
    }

    public RedBlackColor Color { get; set; }
}
