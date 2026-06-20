using System.Collections;

namespace TreeDataStructures;

public abstract class BinarySearchTreeBase<TKey, TValue, TNode> : IEnumerable<TreeEntry<TKey, TValue>>
    where TNode : BinaryTreeNode<TKey, TValue, TNode>
{
    private readonly IComparer<TKey> _comparer;

    protected BinarySearchTreeBase(IComparer<TKey>? comparer = null)
    {
        _comparer = comparer ?? Comparer<TKey>.Default;
    }

    public int Count { get; protected set; }

    public bool IsEmpty => Count == 0;

    protected TNode? Root { get; set; }

    public virtual bool Insert(TKey key, TValue value)
    {
        ValidateKey(key);

        if (Root is null)
        {
            Root = CreateNode(key, value);
            Count = 1;
            OnNodeInserted(Root);
            return true;
        }

        TNode? current = Root;
        TNode? parent = null;
        int compareResult = 0;

        while (current is not null)
        {
            parent = current;
            compareResult = Compare(key, current.Key);

            if (compareResult == 0)
            {
                current.Value = value;
                OnNodeFound(current);
                OnValueReplaced(current);
                return false;
            }

            current = compareResult < 0 ? current.Left : current.Right;
        }

        TNode newNode = CreateNode(key, value);
        newNode.Parent = parent;

        if (compareResult < 0)
        {
            parent!.Left = newNode;
        }
        else
        {
            parent!.Right = newNode;
        }

        Count++;
        UpdateUpwards(parent);
        OnNodeInserted(newNode);
        return true;
    }

    public virtual bool Remove(TKey key)
    {
        ValidateKey(key);

        TNode? node = FindNode(key, out TNode? lastVisited);
        if (node is null)
        {
            OnNodeNotFound(lastVisited);
            return false;
        }

        TNode? updateStart = RemoveNodeFromTree(node);
        Count--;
        UpdateUpwards(updateStart);
        OnNodeRemoved(updateStart);
        return true;
    }

    public bool ContainsKey(TKey key)
    {
        return TryGetValue(key, out _);
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        ValidateKey(key);

        TNode? node = FindNode(key, out TNode? lastVisited);
        if (node is null)
        {
            value = default!;
            OnNodeNotFound(lastVisited);
            return false;
        }

        value = node.Value;
        OnNodeFound(node);
        return true;
    }

    public TValue GetValueOrDefault(TKey key, TValue defaultValue = default!)
    {
        return TryGetValue(key, out TValue value) ? value : defaultValue;
    }

    public bool Update(TKey key, TValue value)
    {
        ValidateKey(key);

        TNode? node = FindNode(key, out TNode? lastVisited);
        if (node is null)
        {
            OnNodeNotFound(lastVisited);
            return false;
        }

        node.Value = value;
        OnNodeFound(node);
        OnValueReplaced(node);
        return true;
    }

    public bool TryGetMinimum(out TreeEntry<TKey, TValue> entry)
    {
        if (Root is null)
        {
            entry = default;
            return false;
        }

        TNode node = GetMinimumNode(Root);
        entry = new TreeEntry<TKey, TValue>(node.Key, node.Value, node.Height);
        return true;
    }

    public bool TryGetMaximum(out TreeEntry<TKey, TValue> entry)
    {
        if (Root is null)
        {
            entry = default;
            return false;
        }

        TNode node = GetMaximumNode(Root);
        entry = new TreeEntry<TKey, TValue>(node.Key, node.Value, node.Height);
        return true;
    }

    public void Clear()
    {
        Root = null;
        Count = 0;
    }

    public TreeEntry<TKey, TValue>[] ToArray(TreeTraversal traversal = TreeTraversal.InOrder)
    {
        List<TreeEntry<TKey, TValue>> result = new();
        foreach (TreeEntry<TKey, TValue> item in Traverse(traversal))
        {
            result.Add(item);
        }

        return result.ToArray();
    }

    public IEnumerable<TreeEntry<TKey, TValue>> Traverse(TreeTraversal traversal)
    {
        return new TreeIterator(Root, traversal);
    }

    public IEnumerator<TreeEntry<TKey, TValue>> GetEnumerator()
    {
        return Traverse(TreeTraversal.InOrder).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    protected abstract TNode CreateNode(TKey key, TValue value);

    protected virtual void OnNodeInserted(TNode node)
    {
    }

    protected virtual void OnNodeRemoved(TNode? startNode)
    {
    }

    protected virtual void OnNodeFound(TNode node)
    {
    }

    protected virtual void OnNodeNotFound(TNode? lastVisited)
    {
    }

    protected virtual void OnValueReplaced(TNode node)
    {
    }

    protected virtual void UpdateNode(TNode node)
    {
        node.Height = Math.Max(GetHeight(node.Left), GetHeight(node.Right)) + 1;
    }

    protected int Compare(TKey left, TKey right)
    {
        return _comparer.Compare(left, right);
    }

    protected static int GetHeight(TNode? node)
    {
        return node?.Height ?? 0;
    }

    protected int GetBalanceFactor(TNode node)
    {
        return GetHeight(node.Left) - GetHeight(node.Right);
    }

    protected void UpdateUpwards(TNode? node)
    {
        while (node is not null)
        {
            UpdateNode(node);
            node = node.Parent;
        }
    }

    protected TNode? FindNode(TKey key, out TNode? lastVisited)
    {
        TNode? current = Root;
        lastVisited = null;

        while (current is not null)
        {
            lastVisited = current;
            int compareResult = Compare(key, current.Key);

            if (compareResult == 0)
            {
                return current;
            }

            current = compareResult < 0 ? current.Left : current.Right;
        }

        return null;
    }

    protected TNode GetMinimumNode(TNode node)
    {
        TNode current = node;
        while (current.Left is not null)
        {
            current = current.Left;
        }

        return current;
    }

    protected TNode GetMaximumNode(TNode node)
    {
        TNode current = node;
        while (current.Right is not null)
        {
            current = current.Right;
        }

        return current;
    }

    protected void Transplant(TNode oldNode, TNode? newNode)
    {
        if (oldNode.Parent is null)
        {
            Root = newNode;
        }
        else if (oldNode == oldNode.Parent.Left)
        {
            oldNode.Parent.Left = newNode;
        }
        else
        {
            oldNode.Parent.Right = newNode;
        }

        if (newNode is not null)
        {
            newNode.Parent = oldNode.Parent;
        }
    }

    protected virtual TNode? RemoveNodeFromTree(TNode node)
    {
        TNode? updateStart;

        if (node.Left is null)
        {
            updateStart = node.Parent;
            Transplant(node, node.Right);
        }
        else if (node.Right is null)
        {
            updateStart = node.Parent;
            Transplant(node, node.Left);
        }
        else
        {
            TNode successor = GetMinimumNode(node.Right);

            if (successor.Parent != node)
            {
                updateStart = successor.Parent;
                Transplant(successor, successor.Right);
                successor.Right = node.Right;
                successor.Right.Parent = successor;
            }
            else
            {
                updateStart = successor;
            }

            Transplant(node, successor);
            successor.Left = node.Left;
            successor.Left.Parent = successor;
            UpdateNode(successor);
        }

        node.Left = null;
        node.Right = null;
        node.Parent = null;
        return updateStart;
    }

    protected TNode SmallLeftRotation(TNode node)
    {
        TNode pivot = node.Right ?? throw new InvalidOperationException("Left rotation requires a right child.");
        TNode? parent = node.Parent;

        node.Right = pivot.Left;
        if (pivot.Left is not null)
        {
            pivot.Left.Parent = node;
        }

        pivot.Left = node;
        node.Parent = pivot;
        pivot.Parent = parent;

        if (parent is null)
        {
            Root = pivot;
        }
        else if (parent.Left == node)
        {
            parent.Left = pivot;
        }
        else
        {
            parent.Right = pivot;
        }

        UpdateNode(node);
        UpdateNode(pivot);
        UpdateUpwards(pivot.Parent);
        return pivot;
    }

    protected TNode SmallRightRotation(TNode node)
    {
        TNode pivot = node.Left ?? throw new InvalidOperationException("Right rotation requires a left child.");
        TNode? parent = node.Parent;

        node.Left = pivot.Right;
        if (pivot.Right is not null)
        {
            pivot.Right.Parent = node;
        }

        pivot.Right = node;
        node.Parent = pivot;
        pivot.Parent = parent;

        if (parent is null)
        {
            Root = pivot;
        }
        else if (parent.Left == node)
        {
            parent.Left = pivot;
        }
        else
        {
            parent.Right = pivot;
        }

        UpdateNode(node);
        UpdateNode(pivot);
        UpdateUpwards(pivot.Parent);
        return pivot;
    }

    protected TNode DoubleLeftRotation(TNode node)
    {
        if (node.Right is null)
        {
            throw new InvalidOperationException("Double left rotation requires a right child.");
        }

        SmallRightRotation(node.Right);
        return SmallLeftRotation(node);
    }

    protected TNode DoubleRightRotation(TNode node)
    {
        if (node.Left is null)
        {
            throw new InvalidOperationException("Double right rotation requires a left child.");
        }

        SmallLeftRotation(node.Left);
        return SmallRightRotation(node);
    }

    protected TNode BigLeftRotation(TNode node)
    {
        return DoubleLeftRotation(node);
    }

    protected TNode BigRightRotation(TNode node)
    {
        return DoubleRightRotation(node);
    }

    private static void ValidateKey(TKey key)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }
    }

    private sealed class TreeIterator : IEnumerable<TreeEntry<TKey, TValue>>, IEnumerator<TreeEntry<TKey, TValue>>
    {
        private readonly TNode? _root;
        private readonly TreeTraversal _traversal;
        private readonly List<TNode> _nodes;
        private int _position;

        public TreeIterator(TNode? root, TreeTraversal traversal)
        {
            _root = root;
            _traversal = traversal;
            _nodes = new List<TNode>();
            _position = -1;
            BuildNodeList(root);
        }

        public TreeEntry<TKey, TValue> Current
        {
            get
            {
                if (_position < 0 || _position >= _nodes.Count)
                {
                    throw new InvalidOperationException("Iterator is not positioned on an element.");
                }

                TNode node = _nodes[_position];
                return new TreeEntry<TKey, TValue>(node.Key, node.Value, node.Height);
            }
        }

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (_position + 1 >= _nodes.Count)
            {
                return false;
            }

            _position++;
            return true;
        }

        public void Reset()
        {
            _position = -1;
        }

        public IEnumerator<TreeEntry<TKey, TValue>> GetEnumerator()
        {
            return new TreeIterator(_root, _traversal);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
        }

        private void BuildNodeList(TNode? node)
        {
            if (node is null)
            {
                return;
            }

            switch (_traversal)
            {
                case TreeTraversal.PreOrder:
                    AddPreOrder(node, reverse: false);
                    break;
                case TreeTraversal.InOrder:
                    AddInOrder(node, reverse: false);
                    break;
                case TreeTraversal.PostOrder:
                    AddPostOrder(node, reverse: false);
                    break;
                case TreeTraversal.ReversePreOrder:
                    AddPreOrder(node, reverse: true);
                    break;
                case TreeTraversal.ReverseInOrder:
                    AddInOrder(node, reverse: true);
                    break;
                case TreeTraversal.ReversePostOrder:
                    AddPostOrder(node, reverse: true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(_traversal), _traversal, "Unknown traversal type.");
            }
        }

        private void AddPreOrder(TNode node, bool reverse)
        {
            _nodes.Add(node);
            AddChildPreOrder(reverse ? node.Right : node.Left, reverse);
            AddChildPreOrder(reverse ? node.Left : node.Right, reverse);
        }

        private void AddChildPreOrder(TNode? node, bool reverse)
        {
            if (node is not null)
            {
                AddPreOrder(node, reverse);
            }
        }

        private void AddInOrder(TNode node, bool reverse)
        {
            AddChildInOrder(reverse ? node.Right : node.Left, reverse);
            _nodes.Add(node);
            AddChildInOrder(reverse ? node.Left : node.Right, reverse);
        }

        private void AddChildInOrder(TNode? node, bool reverse)
        {
            if (node is not null)
            {
                AddInOrder(node, reverse);
            }
        }

        private void AddPostOrder(TNode node, bool reverse)
        {
            AddChildPostOrder(reverse ? node.Right : node.Left, reverse);
            AddChildPostOrder(reverse ? node.Left : node.Right, reverse);
            _nodes.Add(node);
        }

        private void AddChildPostOrder(TNode? node, bool reverse)
        {
            if (node is not null)
            {
                AddPostOrder(node, reverse);
            }
        }
    }
}
