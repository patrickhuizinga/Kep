namespace BranchAndBound;

public interface INodeSelection<TNode>
    where TNode : INode
{
    void Update(TNode parent, IEnumerable<TNode> newChildren);
    TNode? TryClaimOne();
}

public interface INodeSelectionFactory
{
    INodeSelection<TNode> Create<TNode>() where TNode : INode;
}
