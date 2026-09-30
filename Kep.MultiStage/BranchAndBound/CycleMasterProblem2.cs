using Gurobi;
using Kep.Runner;

namespace Kep.MultiStage.BranchAndBound;

/// <summary>
/// Represents the Cycle formulation of the KEP.
/// </summary>
public class CycleMasterProblem2 : IBranchAndBoundProblem<CycleMasterProblem2>
{
    public const double Tolerance = 1e-6;

    private readonly GRBEnv _env;
    private readonly bool[,] _a;
    private readonly double[,] _w;
    private readonly int _k;
    private readonly List<int[]> _allCycles;
    private readonly List<int[]> _excludedSolutions;
    private readonly List<(int index, double x, int[] cycle)> _usedColumns = [];

    public CycleMasterProblem2(GRBEnv env, bool[,] A, double[,] w, int k)
    {
        _env = env;
        _a = A;
        _w = w;
        _k = k;
        
        _allCycles = CycleFormulation.GetAllCycles(_a, _k).ToList();
        _excludedSolutions = [];
    }

    public CycleMasterProblem2(GRBEnv env, bool[,] A, double[,] w, int k, List<int[]> allCycles, List<int[]> excludedSolutions)
    {
        _env = env;
        _a = A;
        _w = w;
        _k = k;
        _allCycles = allCycles;
        _excludedSolutions = excludedSolutions;
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

        using var grbModel = CreateModel();
        
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
            .Select((x, i) => (i, x, cycle: _allCycles[i]))
            .Where(t => IsNonZero(t.x));

        _usedColumns.AddRange(selectedColumns);

        // ref bool isAlreadyFound = ref FoundSolutions.GetNode(UsedCycles, _w.LengthI());
        // if (isAlreadyFound)
        //     return BranchAndBound.ProblemResult.Infeasible;
        //
        // isAlreadyFound = true;
        return global::BranchAndBound.ProblemResult.Candidate(grbModel.ObjVal);
    }

    private GRBModel CreateModel()
    {
        var problem = new GRBModel(_env);
        
        var constraints = problem.AddConstrs(_w.LengthI());
        foreach (var constr in constraints)
        {
            constr.Sense = GRB.LESS_EQUAL;
            constr.RHS = 1;
        }
        
        var solutionConstraints = problem.AddConstrs(_excludedSolutions.Count);
        for (var i = 0; i < solutionConstraints.Length; i++)
        {
            var constraint = solutionConstraints[i];
            constraint.Sense = GRB.LESS_EQUAL;
            constraint.RHS = _excludedSolutions[i].Length - 1;
        }

        for (var j = 0; j < _allCycles.Count; j++)
        {
            var cycle = _allCycles[j];
            var objectiveCoefficient = 0.0;
            var cycleConstraints = new List<GRBConstr>(cycle.Length);

            var prevNode = cycle.Last();
            foreach (var node in cycle)
            {
                objectiveCoefficient -= _w[prevNode, node];
                cycleConstraints.Add(constraints[node]);

                prevNode = node;
            }

            for (var i = 0; i < solutionConstraints.Length; i++)
            {
                if (_excludedSolutions[i].Contains(j))
                    cycleConstraints.Add(solutionConstraints[i]);
            }

            problem.AddVar(0, 1, objectiveCoefficient, GRB.BINARY, cycleConstraints.ToArray(), null, "c");
        }

        return problem;
    }

    public IEnumerable<CycleMasterProblem2> Branch()
    {
        throw new InvalidOperationException("cycle formulation is always a candidate");
    }

    public IEnumerable<CycleMasterProblem2> BranchCandidate()
    {
        var solution = _usedColumns.Select(t => t.index).ToArray();

        var excludedSolutions = new List<int[]>(_excludedSolutions.Count + 1);
        excludedSolutions.AddRange(_excludedSolutions);
        excludedSolutions.Add(solution);

        yield return new CycleMasterProblem2(_env, _a, _w, _k, _allCycles, excludedSolutions);
    }

    private static bool IsNonZero(double x) => x < (0 - Tolerance) || (0 + Tolerance) < x;
}
