namespace BranchAndBound;

/// <summary>
/// Yields the nodes in reverse order of depth (deepest last), with lower-bound as a tiebreaker.
/// </summary>
public sealed class BreadthFirstNodeSelection : INodeSelectionFactory
{
    public INodeSelection<TNode> Create<TNode>() where TNode : INode
    {
        return new BreadthFirstNodeSelection<TNode>();
    }
}

/// <summary>
/// Yields the nodes in reverse order of depth (deepest last), with lower-bound as a tiebreaker.
/// </summary>
public sealed class BreadthFirstNodeSelection<TNode> : INodeSelection<TNode> where TNode : INode
{
    private readonly PriorityQueue<TNode, (int, double)> _nodes = new();
    
    public void Update(TNode parent, IEnumerable<TNode> newChildren)
    {
        var depth = FindDepth(parent);
        var priority = parent.Priority;
        
        lock (_nodes)
        {
            _nodes.EnqueueRange(newChildren, (depth, priority));
        }
    }

    private static int FindDepth(INode node)
    {
        var depth = 0;
        while (node.Parent != null)
        {
            node = node.Parent;
            depth++;
        }
        return depth;
    }

    public TNode? TryClaimOne()
    {
        lock (_nodes)
        {
            while (_nodes.TryDequeue(out var node, out _))
                if (node.TryClaim())
                    return node;
        }

        return default;
    }
}
