using Kep.MultiStage.TwoStage;

namespace Kep.MultiStage.BranchAndBound;

public class SortedEnumerableFormulation<TProblem> : IMultiFormulation
    where TProblem : global::BranchAndBound.IProblem<TProblem>, IBranchAndBoundProblem<TProblem>
{
    private readonly global::BranchAndBound.INodeSelectionFactory _selectionFactory;
    private readonly Func<bool[,], double[,], TProblem> _problemFactory;

    public SortedEnumerableFormulation(
        global::BranchAndBound.INodeSelectionFactory selectionFactory,
        Func<bool[,], double[,], TProblem> problemFactory)
    {
        _selectionFactory = selectionFactory;
        _problemFactory = problemFactory;
    }

    public SortedEnumerableFormulation(
        Func<bool[,], double[,], TProblem> problemFactory)
        : this(new global::BranchAndBound.BestFirstNodeSelection(), problemFactory)
    {
    }

    public IEnumerable<(double objective, IReadOnlyCollection<int[]> cycles)> SolveMany(
        bool[,] hasArc, double[,] weights, int maxSolutions)
    {
        var f = new global::BranchAndBound.SortedEnumerableSolver(_selectionFactory);
        var problem = _problemFactory(hasArc, weights);
        return f.SolveAll(problem)
            .Select(x => (-x.Objective, x.UsedCycles.ToReadOnlyCollection()))
            .Take(maxSolutions);
    }
}
