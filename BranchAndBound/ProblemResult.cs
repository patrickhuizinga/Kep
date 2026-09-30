namespace BranchAndBound;

public sealed class ProblemResult
{
    private ProblemResult(NodeState state, double objective, double lowerBound)
    {
        State = state;
        Objective = objective;
        LowerBound = lowerBound;
    }

    public static ProblemResult Relaxed(double objective, double lowerBound) => new(NodeState.Relaxed, objective, lowerBound);
    public static ProblemResult Candidate(double objective) => new(NodeState.Candidate, objective, objective);

    public static ProblemResult Infeasible => new(NodeState.Infeasible, double.PositiveInfinity, double.PositiveInfinity);

    public double Objective { get; }
    
    public double LowerBound { get; }

    public NodeState State { get; }

    public bool IsRelaxed => State == NodeState.Relaxed;
    public bool IsCandidate => State == NodeState.Candidate;
    public bool IsInfeasible => State == NodeState.Infeasible;
    
    public override string ToString()
    {
        switch (State)
        {
            case NodeState.Relaxed:
                return $"Relaxed:   Obj={Objective},  LB={LowerBound}";
            case NodeState.Candidate:
                return $"Candidate: Obj={Objective}";
            case NodeState.Infeasible:
                return "Infeasible";
            default:
                return State.ToString();
        }
    }
}
