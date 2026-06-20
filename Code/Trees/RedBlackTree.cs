namespace TreeDataStructures;

public class RedBlackTree<TKey, TValue>
    : BinarySearchTreeBase<TKey, TValue, RedBlackTreeNode<TKey, TValue>>
{
    public RedBlackTree(IComparer<TKey>? comparer = null)
        : base(comparer)
    {
    }

    protected override RedBlackTreeNode<TKey, TValue> CreateNode(TKey key, TValue value)
    {
        return new RedBlackTreeNode<TKey, TValue>(key, value);
    }

    protected override void OnNodeInserted(RedBlackTreeNode<TKey, TValue> node)
    {
        FixAfterInsert(node);
    }

    public override bool Remove(TKey key)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        RedBlackTreeNode<TKey, TValue>? node = FindNode(key, out RedBlackTreeNode<TKey, TValue>? lastVisited);
        if (node is null)
        {
            OnNodeNotFound(lastVisited);
            return false;
        }

        RedBlackTreeNode<TKey, TValue> movedOrDeleted = node;
        RedBlackColor originalColor = movedOrDeleted.Color;
        RedBlackTreeNode<TKey, TValue>? fixNode;
        RedBlackTreeNode<TKey, TValue>? fixParent;

        if (node.Left is null)
        {
            fixNode = node.Right;
            fixParent = node.Parent;
            Transplant(node, node.Right);
        }
        else if (node.Right is null)
        {
            fixNode = node.Left;
            fixParent = node.Parent;
            Transplant(node, node.Left);
        }
        else
        {
            movedOrDeleted = GetMinimumNode(node.Right);
            originalColor = movedOrDeleted.Color;
            fixNode = movedOrDeleted.Right;

            if (movedOrDeleted.Parent == node)
            {
                fixParent = movedOrDeleted;
                if (fixNode is not null)
                {
                    fixNode.Parent = movedOrDeleted;
                }
            }
            else
            {
                fixParent = movedOrDeleted.Parent;
                Transplant(movedOrDeleted, movedOrDeleted.Right);
                movedOrDeleted.Right = node.Right;
                movedOrDeleted.Right.Parent = movedOrDeleted;
            }

            Transplant(node, movedOrDeleted);
            movedOrDeleted.Left = node.Left;
            movedOrDeleted.Left.Parent = movedOrDeleted;
            movedOrDeleted.Color = node.Color;
            UpdateNode(movedOrDeleted);
        }

        node.Left = null;
        node.Right = null;
        node.Parent = null;
        Count--;

        RedBlackTreeNode<TKey, TValue>? updateStart = fixParent;
        if (updateStart is null)
        {
            updateStart = fixNode;
        }

        if (updateStart is null)
        {
            updateStart = Root;
        }

        UpdateUpwards(updateStart);

        if (originalColor == RedBlackColor.Black)
        {
            FixAfterDelete(fixNode, fixParent);
        }

        if (Root is not null)
        {
            Root.Color = RedBlackColor.Black;
            UpdateUpwards(Root);
        }

        return true;
    }

    private void FixAfterInsert(RedBlackTreeNode<TKey, TValue> node)
    {
        RedBlackTreeNode<TKey, TValue> current = node;
        current.Color = RedBlackColor.Red;

        while (current.Parent is { Color: RedBlackColor.Red } parent)
        {
            RedBlackTreeNode<TKey, TValue> grandparent = parent.Parent!;

            if (parent == grandparent.Left)
            {
                RedBlackTreeNode<TKey, TValue>? uncle = grandparent.Right;

                if (ColorOf(uncle) == RedBlackColor.Red)
                {
                    parent.Color = RedBlackColor.Black;
                    uncle!.Color = RedBlackColor.Black;
                    grandparent.Color = RedBlackColor.Red;
                    current = grandparent;
                }
                else
                {
                    if (current == parent.Right)
                    {
                        current = parent;
                        SmallLeftRotation(current);
                        parent = current.Parent!;
                        grandparent = parent.Parent!;
                    }

                    parent.Color = RedBlackColor.Black;
                    grandparent.Color = RedBlackColor.Red;
                    SmallRightRotation(grandparent);
                }
            }
            else
            {
                RedBlackTreeNode<TKey, TValue>? uncle = grandparent.Left;

                if (ColorOf(uncle) == RedBlackColor.Red)
                {
                    parent.Color = RedBlackColor.Black;
                    uncle!.Color = RedBlackColor.Black;
                    grandparent.Color = RedBlackColor.Red;
                    current = grandparent;
                }
                else
                {
                    if (current == parent.Left)
                    {
                        current = parent;
                        SmallRightRotation(current);
                        parent = current.Parent!;
                        grandparent = parent.Parent!;
                    }

                    parent.Color = RedBlackColor.Black;
                    grandparent.Color = RedBlackColor.Red;
                    SmallLeftRotation(grandparent);
                }
            }
        }

        Root!.Color = RedBlackColor.Black;
    }

    private void FixAfterDelete(
        RedBlackTreeNode<TKey, TValue>? node,
        RedBlackTreeNode<TKey, TValue>? parent)
    {
        RedBlackTreeNode<TKey, TValue>? current = node;
        RedBlackTreeNode<TKey, TValue>? currentParent = parent;

        while (current != Root && ColorOf(current) == RedBlackColor.Black)
        {
            if (currentParent is null)
            {
                break;
            }

            if (current == currentParent.Left)
            {
                RedBlackTreeNode<TKey, TValue>? sibling = currentParent.Right;

                if (ColorOf(sibling) == RedBlackColor.Red)
                {
                    sibling!.Color = RedBlackColor.Black;
                    currentParent.Color = RedBlackColor.Red;
                    SmallLeftRotation(currentParent);
                    sibling = currentParent.Right;
                }

                if (LeftColorOf(sibling) == RedBlackColor.Black &&
                    RightColorOf(sibling) == RedBlackColor.Black)
                {
                    if (sibling is not null)
                    {
                        sibling.Color = RedBlackColor.Red;
                    }

                    current = currentParent;
                    currentParent = current.Parent;
                }
                else
                {
                    if (RightColorOf(sibling) == RedBlackColor.Black)
                    {
                        if (sibling is not null && sibling.Left is not null)
                        {
                            sibling.Left.Color = RedBlackColor.Black;
                        }

                        if (sibling is not null)
                        {
                            sibling.Color = RedBlackColor.Red;
                            SmallRightRotation(sibling);
                        }

                        sibling = currentParent.Right;
                    }

                    if (sibling is not null)
                    {
                        sibling.Color = currentParent.Color;
                    }

                    currentParent.Color = RedBlackColor.Black;

                    if (sibling is not null && sibling.Right is not null)
                    {
                        sibling.Right.Color = RedBlackColor.Black;
                    }

                    SmallLeftRotation(currentParent);
                    current = Root;
                    currentParent = null;
                }
            }
            else
            {
                RedBlackTreeNode<TKey, TValue>? sibling = currentParent.Left;

                if (ColorOf(sibling) == RedBlackColor.Red)
                {
                    sibling!.Color = RedBlackColor.Black;
                    currentParent.Color = RedBlackColor.Red;
                    SmallRightRotation(currentParent);
                    sibling = currentParent.Left;
                }

                if (RightColorOf(sibling) == RedBlackColor.Black &&
                    LeftColorOf(sibling) == RedBlackColor.Black)
                {
                    if (sibling is not null)
                    {
                        sibling.Color = RedBlackColor.Red;
                    }

                    current = currentParent;
                    currentParent = current.Parent;
                }
                else
                {
                    if (LeftColorOf(sibling) == RedBlackColor.Black)
                    {
                        if (sibling is not null && sibling.Right is not null)
                        {
                            sibling.Right.Color = RedBlackColor.Black;
                        }

                        if (sibling is not null)
                        {
                            sibling.Color = RedBlackColor.Red;
                            SmallLeftRotation(sibling);
                        }

                        sibling = currentParent.Left;
                    }

                    if (sibling is not null)
                    {
                        sibling.Color = currentParent.Color;
                    }

                    currentParent.Color = RedBlackColor.Black;

                    if (sibling is not null && sibling.Left is not null)
                    {
                        sibling.Left.Color = RedBlackColor.Black;
                    }

                    SmallRightRotation(currentParent);
                    current = Root;
                    currentParent = null;
                }
            }
        }

        if (current is not null)
        {
            current.Color = RedBlackColor.Black;
        }
    }

    private static RedBlackColor ColorOf(RedBlackTreeNode<TKey, TValue>? node)
    {
        if (node is null)
        {
            return RedBlackColor.Black;
        }

        return node.Color;
    }

    private static RedBlackColor LeftColorOf(RedBlackTreeNode<TKey, TValue>? node)
    {
        if (node is null)
        {
            return RedBlackColor.Black;
        }

        return ColorOf(node.Left);
    }

    private static RedBlackColor RightColorOf(RedBlackTreeNode<TKey, TValue>? node)
    {
        if (node is null)
        {
            return RedBlackColor.Black;
        }

        return ColorOf(node.Right);
    }
}
