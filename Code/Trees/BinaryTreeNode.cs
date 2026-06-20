namespace TreeDataStructures;

public class BinaryTreeNode<TKey, TValue, TNode>
    where TNode : BinaryTreeNode<TKey, TValue, TNode>
{
    public BinaryTreeNode(TKey key, TValue value)
    {
        Key = key;
        Value = value;
        Height = 1;
    }

    public TKey Key { get; set; }

    public TValue Value { get; set; }

    public TNode? Left { get; set; }

    public TNode? Right { get; set; }

    public TNode? Parent { get; set; }

    public int Height { get; set; }
}
