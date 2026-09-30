using BranchAndBound;

namespace Kep.Test;

public class GreedyKnapsackProblem : IProblem<GreedyKnapsackProblem>
{
    public const double Epsilon = 1e-6;

    private readonly int _capacity = 10;
    private readonly int[] _weights = [3, 4, 6, 7, 8];
    private readonly int[] _values = [-1, -4, -6, -8, -9];
    private readonly double[] _x = new double[5];
    private readonly bool[] _branchMask;
    
    public GreedyKnapsackProblem()
    {
        _branchMask = new bool[5];
    }
    
    private GreedyKnapsackProblem(bool[] branchMask)
    {
        _branchMask = branchMask;
    }

    public double Solve()
    {
        double remainingCapacity = _capacity;
        double objective = 0;
        
        for (int i = 0; i < _weights.Length; i++)
        {
            if (_branchMask[i])
            {
                remainingCapacity -= _x[i]*_weights[i];
                objective += _x[i]*_values[i];
            }
        }
        
        if (remainingCapacity < 0)
            return double.PositiveInfinity;
        if (remainingCapacity == 0)
            return objective;
        
        var items = _weights
            .Zip(_values)
            .Zip(Enumerable.Range(0, _weights.Length), (t, i) => (weight: t.First, value: t.Second, i))
            .OrderBy(t => (double)t.value/t.weight)
            // There is a greater chance we can add small items after adding large ones, than the other way around.
            // It doesn't matter for the test cases, unless you flip the weights and values, but who would do that...
            .ThenByDescending(t => t.weight);
        
        foreach (var (weight, value, i) in items)
        {
            if (_branchMask[i])
                continue;
            
            if (remainingCapacity < weight)
            {
                var d = remainingCapacity / weight;
                _x[i] = d;
                objective += d*value;
                break;
            }
            
            remainingCapacity -= weight;
            objective += value;
            _x[i] = 1;
                
            if (remainingCapacity == 0)
                break;
        }
        
        return objective;
    }
    
    public ProblemResult Minimize()
    {
        var objective = Solve();
        if (double.IsPositiveInfinity(objective))
            return ProblemResult.Infeasible;
        
        if (_x.All(v => v < Epsilon || 1 - Epsilon < v))
            return ProblemResult.Candidate(objective);

        var lowerBound = double.Ceiling(objective - Epsilon);
        return ProblemResult.Relaxed(objective, lowerBound);
    }

    public IEnumerable<GreedyKnapsackProblem> Branch()
    {
        // this method will only be called if the solution was not integer (there is at least one fractional variable)
        // for the greedy knapsack problem, there can be at most one fractional variable
        // create two copies of the problem, one with the fractional variable set to 1, and one with it set to 0
        // mark that variable as a branch variable, so Solve() will have that value 'hardcoded' in the objective function
        // additionally, copy all already-branched variables in this method
        
        var branchMask = (bool[])_branchMask.Clone();
        var first = new GreedyKnapsackProblem(branchMask);
        var second = new GreedyKnapsackProblem(branchMask);

        for (int i = 0; i < _x.Length; i++)
        {
            if (_branchMask[i])
            {
                first._x[i] = _x[i];
                second._x[i] = _x[i];
                continue;
            }
            
            if (_x[i] < Epsilon)
                continue;
            if (_x[i] > 1 - Epsilon)
                continue;
            
            branchMask[i] = true;
            first._x[i] = 1;
            second._x[i] = 0;
        }
        
        yield return first;
        yield return second;
    }

    public IEnumerable<GreedyKnapsackProblem> BranchCandidate()
    {
        // this method will only be called if the solution was not integer (there is at least one fractional variable)
        // for the greedy knapsack problem, there can be at most one fractional variable
        // create two copies of the problem, one with the fractional variable set to 1, and one with it set to 0
        // mark that variable as a branch variable, so Solve() will have that value 'hardcoded' in the objective function
        // additionally, copy all already-branched variables in this method
        
        var branchMask = (bool[])_branchMask.Clone();
        var branchProblem = new GreedyKnapsackProblem(branchMask);
        _x.CopyTo(branchProblem._x);

        for (int i = 0; i < _x.Length; i++)
        {
            if (_branchMask[i] || _x[i] < 0 + Epsilon)
                continue;
            
            branchMask[i] = true;
            branchProblem._x[i] = 0;
            yield return branchProblem;
            break;
        }
    }

    public override string ToString()
    {
        return string.Join(", ", _x) + " : " + Minimize();
    }
}
