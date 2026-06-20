namespace TreeDataStructures;

public class AvlTree<TKey, TValue>
    : BinarySearchTreeBase<TKey, TValue, AvlTreeNode<TKey, TValue>>
{
    public AvlTree(IComparer<TKey>? comparer = null)
        : base(comparer)
    {
    }

    protected override AvlTreeNode<TKey, TValue> CreateNode(TKey key, TValue value)
    {
        return new AvlTreeNode<TKey, TValue>(key, value);
    }

    protected override void OnNodeInserted(AvlTreeNode<TKey, TValue> node)
    {
        RebalanceFrom(node);
    }

    protected override void OnNodeRemoved(AvlTreeNode<TKey, TValue>? startNode)
    {
        RebalanceFrom(startNode);
    }

    private void RebalanceFrom(AvlTreeNode<TKey, TValue>? node)
    {
        AvlTreeNode<TKey, TValue>? current = node;

        while (current is not null)
        {
            UpdateNode(current);
            int balance = GetBalanceFactor(current);
            AvlTreeNode<TKey, TValue> subtreeRoot = current;

            if (balance > 1)
            {
                if (current.Left is not null && GetBalanceFactor(current.Left) < 0)
                {
                    SmallLeftRotation(current.Left);
                }

                subtreeRoot = SmallRightRotation(current);
            }
            else if (balance < -1)
            {
                if (current.Right is not null && GetBalanceFactor(current.Right) > 0)
                {
                    SmallRightRotation(current.Right);
                }

                subtreeRoot = SmallLeftRotation(current);
            }

            current = subtreeRoot.Parent;
        }
    }
}

public class AVLTree<TKey, TValue> : AvlTree<TKey, TValue>
{
    public AVLTree(IComparer<TKey>? comparer = null)
        : base(comparer)
    {
    }
}
