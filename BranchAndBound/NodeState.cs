namespace BranchAndBound;

public enum NodeState
{
    Initialized,
    Solving,
    Relaxed,
    Candidate,
    Infeasible,
    Pruned
}
