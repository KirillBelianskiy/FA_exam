namespace TreeDataStructures;

public class SplayTree<TKey, TValue>
    : BinarySearchTreeBase<TKey, TValue, SplayTreeNode<TKey, TValue>>
{
    public SplayTree(IComparer<TKey>? comparer = null)
        : base(comparer)
    {
    }

    protected override SplayTreeNode<TKey, TValue> CreateNode(TKey key, TValue value)
    {
        return new SplayTreeNode<TKey, TValue>(key, value);
    }

    protected override void OnNodeInserted(SplayTreeNode<TKey, TValue> node)
    {
        Splay(node);
    }

    protected override void OnNodeFound(SplayTreeNode<TKey, TValue> node)
    {
        Splay(node);
    }

    protected override void OnNodeNotFound(SplayTreeNode<TKey, TValue>? lastVisited)
    {
        if (lastVisited is not null)
        {
            Splay(lastVisited);
        }
    }

    public override bool Remove(TKey key)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        SplayTreeNode<TKey, TValue>? node = FindNode(key, out SplayTreeNode<TKey, TValue>? lastVisited);
        if (node is null)
        {
            OnNodeNotFound(lastVisited);
            return false;
        }

        Splay(node);

        SplayTreeNode<TKey, TValue>? leftTree = Root!.Left;
        SplayTreeNode<TKey, TValue>? rightTree = Root.Right;

        if (leftTree is not null)
        {
            leftTree.Parent = null;
        }

        if (rightTree is not null)
        {
            rightTree.Parent = null;
        }

        Root.Left = null;
        Root.Right = null;

        if (leftTree is null)
        {
            Root = rightTree;
        }
        else
        {
            Root = leftTree;
            SplayTreeNode<TKey, TValue> maximum = GetMaximumNode(leftTree);
            Splay(maximum);
            Root!.Right = rightTree;
            if (rightTree is not null)
            {
                rightTree.Parent = Root;
            }

            UpdateNode(Root);
        }

        Count--;
        UpdateUpwards(Root);
        return true;
    }

    private void Splay(SplayTreeNode<TKey, TValue> node)
    {
        while (node.Parent is not null)
        {
            SplayTreeNode<TKey, TValue> parent = node.Parent;
            SplayTreeNode<TKey, TValue>? grandparent = parent.Parent;

            if (grandparent is null)
            {
                if (node == parent.Left)
                {
                    SmallRightRotation(parent);
                }
                else
                {
                    SmallLeftRotation(parent);
                }
            }
            else if (node == parent.Left && parent == grandparent.Left)
            {
                SmallRightRotation(grandparent);
                SmallRightRotation(parent);
            }
            else if (node == parent.Right && parent == grandparent.Right)
            {
                SmallLeftRotation(grandparent);
                SmallLeftRotation(parent);
            }
            else if (node == parent.Right && parent == grandparent.Left)
            {
                SmallLeftRotation(parent);
                SmallRightRotation(grandparent);
            }
            else
            {
                SmallRightRotation(parent);
                SmallLeftRotation(grandparent);
            }
        }
    }
}
