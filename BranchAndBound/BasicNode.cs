namespace BranchAndBound;

public class BasicNode<TProblem>(BasicNode<TProblem>? parent, TProblem problem) : INode
    where TProblem : IProblem<TProblem>
{
    private int _state = (int)NodeState.Initialized;
    
    public INode? Parent => parent;

    public TProblem Problem => problem;

    public NodeState State => (NodeState)Interlocked.CompareExchange(ref _state, 0, 0);
        
    public double Objective { get; private set; } = double.NaN;
        
    public BasicNode<TProblem>[] Children { get; private set; } = [];

    public double LowerBound { get; private set; } = parent?.LowerBound ?? double.NegativeInfinity;
    
    public double Priority => LowerBound;

    public bool TryClaim()
    {
        return TryUpdateState(NodeState.Initialized, NodeState.Solving);
    }

    public void Minimize()
    {
        var result = Problem.Minimize();

        Objective = result.Objective;
        LowerBound = result.LowerBound;
        parent?.UpdateLowerBound();
            
        if (result.IsInfeasible)
        {
            SetState(NodeState.Infeasible);
            return;
        }
            
        if (result.IsCandidate)
        {
            TryUpdateState(NodeState.Solving, NodeState.Candidate);
            return;
        }

        // Something updated our state while we were solving
        // Whatever it is (probably Pruned), it supersedes
        if (!TryUpdateState(NodeState.Solving, NodeState.Relaxed))
            return;

        Children = Problem.Branch()
            .Select(p => new BasicNode<TProblem>(parent: this, p))
            .ToArray();
        
        if (Children.Length == 0)
            SetState(NodeState.Infeasible);
    }

    /// <summary>
    /// Sets the lower bound to the smallest of the children's lower bounds.
    /// </summary>
    private void UpdateLowerBound()
    {
        // deal with the (unlikely) case of 0 children
        if (Children.Length == 0)
            return;
            
        var min = Children.Min(n => n.LowerBound);
            
        // initially a child's lower bound is set to the parent's lower bound,
        // so if it's still equal (or less), then we don't need to update ours, nor our parent's
        if (min <= LowerBound)
            return;
            
        LowerBound = min;
        parent?.UpdateLowerBound();
    }

    public void Prune(double upperBound)
    {
        if (upperBound <= LowerBound)
        {
            SetState(NodeState.Pruned);
            Children = [];
        }

        foreach (var child in Children)
            child.Prune(upperBound);
    }
        
    private void SetState(NodeState newState)
    {
        Interlocked.Exchange(ref _state, (int)newState);
    }

    private bool TryUpdateState(NodeState oldState, NodeState newState)
    {
        var comparand = (int)oldState;
        var prevState = Interlocked.CompareExchange(ref _state, (int)newState, comparand);
        return prevState == comparand;
    }
}
