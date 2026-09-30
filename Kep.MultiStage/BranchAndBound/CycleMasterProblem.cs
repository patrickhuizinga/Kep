using Gurobi;
using Kep.Runner;
using Kep.MultiStage.Glorie2014;

namespace Kep.MultiStage.BranchAndBound;

/// <summary>
/// Represents the Cycle formulation of the KEP.
/// </summary>
public class CycleMasterProblem : IBranchAndBoundProblem<CycleMasterProblem>
{
    public const double Tolerance = 1e-6;

    private readonly GRBEnv _env;
    private readonly bool[,] _a;
    private readonly double[,] _w;
    private readonly int _k;
    private readonly List<int[]> _allCycles;
    private readonly List<(double x, int[] cycle)> _usedColumns = [];

    private static readonly GraphMap<bool> FoundSolutions = new();

    public CycleMasterProblem(GRBEnv env, bool[,] A, double[,] w, int k)
    {
        _env = env;
        _a = A;
        _w = w;
        _k = k;
        
        _allCycles = CycleFormulation.GetAllCycles(_a, _k).ToList();
    }

    public CycleMasterProblem(GRBEnv env, bool[,] A, double[,] w, int k, List<int[]> allCycles)
    {
        _env = env;
        _a = A;
        _w = w;
        _k = k;
        _allCycles = allCycles;
    }

    public double Objective { get; private set; }

    public IEnumerable<int[]> UsedCycles => _usedColumns.Select(t => t.cycle);

    /// <summary>
    /// Refers to the number of times <see cref="Minimize"/> has been called,
    /// not to the number of times the master problem LP has been minimized.
    /// </summary>
    public static int MinimizeCount;

    public global::BranchAndBound.ProblemResult Minimize()
    {
        MinimizeCount++;
        
        _usedColumns.Clear();

        using var grbModel = CreateModel(_allCycles);
        
        grbModel.Optimize();
        switch (grbModel.Status)
        {
            case GRB.Status.INFEASIBLE:
                Console.WriteLine("infeasible");
                return global::BranchAndBound.ProblemResult.Infeasible;
            case GRB.Status.INF_OR_UNBD:
                Console.WriteLine("infeasible or unbounded");
                return global::BranchAndBound.ProblemResult.Infeasible;
            case GRB.Status.OPTIMAL:
                break;
            default:
                Console.WriteLine("something went wrong: " + grbModel.Status);
                return global::BranchAndBound.ProblemResult.Infeasible;
        }

        
        Objective = grbModel.ObjVal;

        var selectedColumns = grbModel.GetXValues()
            .Zip(_allCycles, (x, cycle) => (x, cycle))
            .Where(t => IsNonZero(t.x));

        _usedColumns.AddRange(selectedColumns);

        ref bool isAlreadyFound = ref FoundSolutions.GetOrAddDefault(UsedCycles, _w.LengthI());
        if (isAlreadyFound)
            return global::BranchAndBound.ProblemResult.Infeasible;

        isAlreadyFound = true;
        return global::BranchAndBound.ProblemResult.Candidate(grbModel.ObjVal);
    }

    private GRBModel CreateModel(IEnumerable<int[]> cycles)
    {
        var problem = new GRBModel(_env);
        
        var constraints = problem.AddConstrs(_w.LengthI());
        foreach (var constr in constraints)
        {
            constr.Sense = GRB.LESS_EQUAL;
            constr.RHS = 1;
        }

        foreach (var cycle in cycles)
        {
            var objectiveCoefficient = 0.0;
            var cycleConstraints = new GRBConstr[cycle.Length];

            var prevNode = cycle.Last();
            for (var i = 0; i < cycle.Length; i++)
            {
                var node = cycle[i];
                objectiveCoefficient -= _w[prevNode, node];
                cycleConstraints[i] = constraints[node];
                
                prevNode = node;
            }

            problem.AddVar(0, 1, objectiveCoefficient, GRB.BINARY, cycleConstraints, null, "c");
        }
        
        return problem;
    }

    public IEnumerable<CycleMasterProblem> Branch()
    {
        throw new InvalidOperationException("cycle formulation is always a candidate");
    }

    public IEnumerable<CycleMasterProblem> BranchCandidate()
    {
        var arcs = _usedColumns
            .SelectMany(col => GetArcs(col.cycle));

        foreach (var arc in arcs)
            yield return CreateBranch(excludedArcs: arc);
        
        _allCycles.Clear();
    }

    private CycleMasterProblem CreateBranch(params (int u, int v)[] excludedArcs)
    {
        var branchA = (bool[,])_a.Clone();

        foreach (var (u, v) in excludedArcs)
            branchA[u, v] = false;

        var allCycles = new List<int[]>(_allCycles);
        RemoveCyclesWithAnyArc(allCycles, excludedArcs);

        return new CycleMasterProblem(_env, branchA, _w, _k, allCycles);
    }

    private static void RemoveCyclesWithAnyArc(List<int[]> cycles, (int u, int v)[] arcs)
    {
        for (var i = cycles.Count - 1; i >= 0; i--)
        {
            var cycle = cycles[i];
                
            foreach (var (u, v) in arcs)
            {
                var index = cycle.IndexOf(u);
                // if the cycle doesn't contain node u, we keep it
                if (index == -1)
                    continue;

                // if the cycle doesn't contain arc (u, v), we keep it
                var nextNode = cycle[(index + 1)%cycle.Length];
                if (nextNode != v)
                    continue;

                // otherwise we drop need the cycle
                cycles.RemoveAt(i);
                break;
            }
        }
    }

    private static IEnumerable<(int u, int v)> GetArcs(int[] cycle)
    {
        var u = cycle[^1];
        foreach (var v in cycle)
        {
            yield return (u, v);
            u = v;
        }
    }

    private static bool IsNonZero(double x) => x < (0 - Tolerance) || (0 + Tolerance) < x;
}
