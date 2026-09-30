namespace BranchAndBound;

public interface IProblemEnumerable<out TProblem> : IEnumerable<TProblem>
{
    public double UpperBound { get; }
    
    /// <summary>
    /// Attempts to decrease <see cref="UpperBound"/> and return true if successful.
    /// </summary>
    public bool TrySetUpperBound(double upperBound);
}
