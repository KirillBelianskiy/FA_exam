namespace TreeDataStructures;

public class Treap<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, TreapNode<TKey, TValue>>
{
    private readonly Func<int> _priorityGenerator;

    public Treap(IComparer<TKey>? comparer = null)
        : this(new Random(), comparer)
    {
    }

    public Treap(Random random, IComparer<TKey>? comparer = null)
        : base(comparer)
    {
        if (random is null)
        {
            throw new ArgumentNullException(nameof(random));
        }

        _priorityGenerator = random.Next;
    }

    public Treap(Func<int> priorityGenerator, IComparer<TKey>? comparer = null)
        : base(comparer)
    {
        if (priorityGenerator is null)
        {
            throw new ArgumentNullException(nameof(priorityGenerator));
        }

        _priorityGenerator = priorityGenerator;
    }

    protected override TreapNode<TKey, TValue> CreateNode(TKey key, TValue value)
    {
        return new TreapNode<TKey, TValue>(key, value, _priorityGenerator());
    }

    protected override void OnNodeInserted(TreapNode<TKey, TValue> node)
    {
        while (node.Parent is not null && node.Parent.Priority < node.Priority)
        {
            if (node == node.Parent.Left)
            {
                SmallRightRotation(node.Parent);
            }
            else
            {
                SmallLeftRotation(node.Parent);
            }
        }
    }

    public override bool Remove(TKey key)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        TreapNode<TKey, TValue>? node = FindNode(key, out TreapNode<TKey, TValue>? lastVisited);
        if (node is null)
        {
            OnNodeNotFound(lastVisited);
            return false;
        }

        while (node.Left is not null || node.Right is not null)
        {
            if (node.Left is null)
            {
                SmallLeftRotation(node);
            }
            else if (node.Right is null)
            {
                SmallRightRotation(node);
            }
            else if (node.Left.Priority > node.Right.Priority)
            {
                SmallRightRotation(node);
            }
            else
            {
                SmallLeftRotation(node);
            }
        }

        TreapNode<TKey, TValue>? parent = node.Parent;
        Transplant(node, null);
        Count--;
        UpdateUpwards(parent);
        OnNodeRemoved(parent);
        return true;
    }
}
