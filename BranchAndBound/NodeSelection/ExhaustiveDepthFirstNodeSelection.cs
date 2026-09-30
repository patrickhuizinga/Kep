namespace BranchAndBound;

/// <summary>
/// Like <see cref="DepthFirstNodeSelection{TNode}"/>, but explores all children of a node before moving on to the next node.
/// </summary>
public sealed class ExhaustiveDepthFirstNodeSelection : INodeSelectionFactory
{
    public INodeSelection<TNode> Create<TNode>() where TNode : INode
    {
        return new ExhaustiveDepthFirstNodeSelection<TNode>();
    }
}

/// <summary>
/// Like <see cref="DepthFirstNodeSelection{TNode}"/>, but explores all children of a node before moving on to the next node.
/// </summary>
public sealed class ExhaustiveDepthFirstNodeSelection<TNode> : INodeSelection<TNode> where TNode : INode
{
    private readonly PriorityQueue<TNode[], (int, double)> _nodes = new();
    private readonly Queue<TNode> _siblings = new();
    
    public void Update(TNode parent, IEnumerable<TNode> newChildren)
    {
        var depth = FindDepth(parent);
        var priority = parent.Priority;

        lock (_nodes)
        {
            _nodes.Enqueue(
                newChildren.ToArray(),
                (-depth, priority));
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
            while (true)
            {
                // first process any remaining siblings of the previously claimed node
                while (_siblings.TryDequeue(out var node))
                    if (node.TryClaim())
                        return node;
                
                // otherwise, fetch the next batch of siblings
                if (_nodes.Count == 0)
                    return default;
                
                var siblings =  _nodes.Dequeue();
                foreach (var node in siblings)
                    _siblings.Enqueue(node);
            }
        }
    }
}
