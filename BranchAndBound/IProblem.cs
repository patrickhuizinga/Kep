namespace BranchAndBound;

public interface IProblem<out TSelf>
    where TSelf : IProblem<TSelf>
{
    /// <summary>
    /// Solves the problem and returns basic result info.
    /// </summary>
    ProblemResult Minimize();
    
    /// <summary>
    /// Returns zero or more child problems.
    /// Will not be called unless <see cref="Minimize"/> returns <see cref="NodeState.Relaxed"/>.
    /// </summary>
    IEnumerable<TSelf> Branch();
    
    /// <summary>
    /// Returns zero or more child problems.
    /// Will not be called unless <see cref="Minimize"/> returns <see cref="NodeState.Candidate"/>.
    /// </summary>
    IEnumerable<TSelf> BranchCandidate();
}
