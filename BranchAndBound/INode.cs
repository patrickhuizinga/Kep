namespace BranchAndBound;

public interface INode
{
    /// <summary>
    /// The parent node of this node, if any.
    /// </summary>
    INode? Parent { get; }
    
    /// <summary>
    /// The (tiebreaker) priority of this node. The lower the value, the higher the priority.
    /// </summary>
    double Priority { get; }
    
    /// <summary>
    /// The current state of this node.
    /// </summary>
    NodeState State { get; }
    
    /// <summary>
    /// Attempts to get the exclusive claim of this node.
    /// If successful, <see cref="State"/> will be set to <see cref="NodeState.Solving"/>.
    /// </summary>
    /// <returns>True if successful.</returns>
    bool TryClaim();
}
