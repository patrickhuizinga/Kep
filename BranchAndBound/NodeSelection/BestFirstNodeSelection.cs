namespace BranchAndBound;

/// <summary>
/// Yields the nodes in order of lower-bound.
/// </summary>
public sealed class BestFirstNodeSelection : INodeSelectionFactory
{
    public INodeSelection<TNode> Create<TNode>() where TNode : INode
    {
        return new BestFirstNodeSelection<TNode>();
    }
}

/// <summary>
/// Yields the nodes in order of lower-bound.
/// </summary>
public sealed class BestFirstNodeSelection<TNode> : INodeSelection<TNode> where TNode : INode
{
    private readonly PriorityQueue<TNode, double> _nodes = new();
    
    public void Update(TNode parent, IEnumerable<TNode> newChildren)
    {
        var priority = parent.Priority;
        
        lock (_nodes)
        {
            _nodes.EnqueueRange(newChildren, priority);
        }
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
