namespace Kep.MultiStage.BranchAndBound;

public interface IBranchAndBoundProblem<out TSelf> : global::BranchAndBound.IProblem<TSelf>
    where TSelf : global::BranchAndBound.IProblem<TSelf>
{
    double Objective { get; }
    IEnumerable<int[]> UsedCycles { get; }
}
