using System.Runtime.CompilerServices;
using BranchAndBound;
using Gurobi;
using Kep.MultiStage.BranchAndBound;
using Kep.Runner;

namespace Kep.MultiStage.Glorie2014;

public class MasterProblem : IBranchAndBoundProblem<MasterProblem>
{
    public const double Tolerance = 1e-6;

    private readonly GRBEnv _env;
    private readonly bool[,] _a;
    private readonly double[,] _w;
    private readonly int _k;
    private readonly List<int[]> _allCycles;
    private readonly List<int[]> _parentsUsedCycles;
    private readonly List<(double x, int[] cycle)> _usedColumns = [];

    private static readonly GraphMap<bool> FoundSolutions = new();

    public MasterProblem(GRBEnv env, bool[,] A, double[,] w, int k)
    {
        _env = env;
        _a = A;
        _w = w;
        _k = k;
        _allCycles = [];
        _parentsUsedCycles = [];
    }

    public MasterProblem(GRBEnv env, bool[,] A, double[,] w, int k, List<int[]> allCycles, List<int[]> parentsUsedCycles)
    {
        _env = env;
        _a = A;
        _w = w;
        _k = k;
        _allCycles = allCycles;
        _parentsUsedCycles = parentsUsedCycles;
    }

    public double Objective { get; private set; }

    public IEnumerable<int[]> UsedCycles => _usedColumns.Select(t => t.cycle);

    /// <summary>
    /// Refers to the number of times <see cref="Minimize"/> has been called,
    /// not to the number of times the master problem LP has been minimized.
    /// </summary>
    public static int MinimizeCount;
    
    public ProblemResult Minimize()
    {
        MinimizeCount++;
        
        _usedColumns.Clear();
        
        using var grbModel = new GRBModel(_env);

        GRBConstr[] constraints = grbModel.AddConstrs(_a.LengthI());
        foreach (var constr in constraints)
        {
            constr.Sense = GRB.LESS_EQUAL;
            constr.RHS = 1;
        }

        // var pricingProblem = new SimplisticPricingProblem(_a, _w, _k);
        // var pricingProblem = new BellmanFordPricingProblem(_a, _w, _k);
        // var pricingProblem = new BellmanFordPricingProblem2(_a, _w, _k);
        // var pricingProblem = new BellmanFordPricingProblem3(_a, _w, _k);
        var pricingProblem = new BellmanFordPricingProblem3Simd(_a, _w, _k);
        // var pricingProblem = new DfsPricingProblem(_a, _w, _k);
        // var pricingProblem = new DfsEarlyExitPricingProblem(_a, _w, _k);

        // initial cycles for master problem
        if (_allCycles.Count == 0)
        {
            if (_parentsUsedCycles.Count == 0)
            {
                var duals = new double[_a.LengthI()];
                _allCycles.AddRange(
                    pricingProblem.SolveMany(duals));
            }
            else
            {
                _allCycles.AddRange(_parentsUsedCycles);
            }
        }
        
        AddCycles(_allCycles);
        
        while (true)
        {   
            grbModel.Optimize();
            switch (grbModel.Status)
            {
                case GRB.Status.INFEASIBLE:
                    Console.WriteLine("infeasible");
                    return ProblemResult.Infeasible;
                case GRB.Status.INF_OR_UNBD:
                    Console.WriteLine("infeasible or unbounded");
                    return ProblemResult.Infeasible;
                case GRB.Status.OPTIMAL:
                    break;
                default:
                    Console.WriteLine("something went wrong: " + grbModel.Status);
                    return ProblemResult.Infeasible;
            }

            var duals = grbModel.GetDuals(constraints);

            var cycles = new List<int[]>(duals.Length);
            cycles.AddRange(
                pricingProblem.SolveMany(duals));
            
            if (cycles.Count == 0)
                break;
            
            AddCycles(cycles);
            _allCycles.AddRange(cycles);
        }

        
        Objective = grbModel.ObjVal;

        var selectedColumns = grbModel.GetXValues()
            .Zip(_allCycles, (x, cycle) => (x, cycle))
            .Where(t => IsNonZero(t.x));

        _usedColumns.AddRange(selectedColumns);

        if (IsIpSolution)
        {
            ref bool isAlreadyFound = ref FoundSolutions.GetOrAddDefault(UsedCycles, _w.LengthI());
            if (isAlreadyFound)
                return ProblemResult.Infeasible;

            isAlreadyFound = true;
            return ProblemResult.Candidate(grbModel.ObjVal);
        }
        
        return ProblemResult.Relaxed(grbModel.ObjVal, grbModel.ObjBound);


        void AddCycles(List<int[]> cycles)
        {
            var lb = new double[cycles.Count];
            // Array.Fill(ub, 0);
            
            var ub = new double[cycles.Count];
            Array.Fill(ub, 1);
            
            var obj = new double[cycles.Count];
            
            var type = new char[cycles.Count];
            Array.Fill(type, GRB.CONTINUOUS);
            
            var name = new string[cycles.Count];
            Array.Fill(name, "col");

            var col = new GRBColumn[cycles.Count];
            
            for (var i = 0; i < cycles.Count; i++)
            {
                var cycle = cycles[i];
                var objectiveCoefficient = GetObjectiveCoefficient(cycle);
                if (objectiveCoefficient == 0)
                {
                    lb[i] = 0;
                    obj[i] = 0;
                    name[i] = "null";
                    col[i] = new GRBColumn();
                    continue;
                }

                obj[i] = objectiveCoefficient;

                col[i] = new GRBColumn();
                foreach (var u in cycle)
                    col[i].AddTerm(1, constraints[u]);
            }

            grbModel.AddVars(lb, ub, obj, type, name, col);
        }
    }

    private double GetObjectiveCoefficient(int[] cycle)
    {
        var objectiveCoefficient = 0.0;
        var prevNode = cycle[^1];
        foreach (var node in cycle)
        {
            if (!_a[prevNode, node])
                return 0;
            
            objectiveCoefficient -= _w[prevNode, node];
            prevNode = node;
        }
        return objectiveCoefficient;
    }

    public IEnumerable<MasterProblem> Branch()
    {
        // get all nodes that are part of some partially selected column,
        // of those get the node that is closest to fully included,
        // then by ties with the smallest (largest x)
        // then by ties with the most out arcs
        
        double[,] xPerArc = new double[_a.LengthI(), _a.LengthJ()];
        foreach (var col in _usedColumns.Where(col => !IsBoolean(col.x)))
        foreach (var (u, v) in GetArcs(col.cycle))
            xPerArc[u, v] += col.x;
        
        double[] xSumPerU = xPerArc.SumPerI();
        double[] xMaxPerU = xPerArc.MaxPerI();

        var selectedNode = Enumerable.Range(0, _a.LengthI())
            .OrderByDescending(u => xSumPerU[u])
            .ThenBy(u => xMaxPerU[u])
            .ThenByDescending(OutArcCount)
            .First();
        
        var selectedArcs = Enumerable.Range(0, _a.LengthJ())
            .Select(v => (u: selectedNode, v))
            .Where(arc => _a[arc.u, arc.v])
            .OrderByDescending(arc => xPerArc[arc.u, arc.v]);

        var arcGroup1 = new List<(int u, int v)>();
        var arcGroup2 = new List<(int u, int v)>();
        var totalX1 = 0.0;
        var totalX2 = 0.0;
        
        foreach (var arc in selectedArcs)
        {
            // try to keep the groups balanced by total x and otherwise by count
            
            if (IsNonZero(xPerArc[arc.u, arc.v]))
            {
                if (totalX1 <= totalX2)
                {
                    arcGroup1.Add(arc);
                    totalX1 += xPerArc[arc.u, arc.v];
                }
                else
                {
                    arcGroup2.Add(arc);
                    totalX2 += xPerArc[arc.u, arc.v];
                }
            }
            else
            {
                if (arcGroup1.Count <= arcGroup2.Count)
                    arcGroup1.Add(arc);
                else
                    arcGroup2.Add(arc);
            }
        }

        yield return CreateBranch(excludedArcs: arcGroup1.ToArray());
        yield return CreateBranch(excludedArcs: arcGroup2.ToArray());

        yield break;

        // returns all outgoing arc
        int OutArcCount(int u)
        {
            int result = 0;
            for (int j = 0; j < _a.LengthJ(); j++)
            {
                if (_a[u, j])
                    result++;
            }
            return result;
        }
    }

    public IEnumerable<MasterProblem> BranchCandidate()
    {
        var arcs = _usedColumns
            .SelectMany(col => GetArcs(col.cycle));

        foreach (var arc in arcs)
            yield return CreateBranch(excludedArcs: arc);
    }

    private MasterProblem CreateBranch(params (int u, int v)[] excludedArcs)
    {
        var branchA = (bool[,])_a.Clone();

        foreach (var (u, v) in excludedArcs)
            branchA[u, v] = false;

        var allCycles = new List<int[]>(_allCycles);
        RemoveCyclesWithAnyArc(allCycles, excludedArcs);

        var parentsUsedCycles = new List<int[]>(_parentsUsedCycles);
        parentsUsedCycles.AddRange(_usedColumns.Select(t => t.cycle));
        RemoveCyclesWithAnyArc(parentsUsedCycles,  excludedArcs);

        return new MasterProblem(_env, branchA, _w, _k, allCycles, parentsUsedCycles);

        static void RemoveCyclesWithAnyArc(List<int[]> cycles, (int u, int v)[] arcs)
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
    }

    private bool IsIpSolution => _usedColumns.All(col => IsBoolean(col.x));

    private static IEnumerable<(int u, int v)> GetArcs(int[] cycle)
    {
        var u = cycle[^1];
        foreach (var v in cycle)
        {
            yield return (u, v);
            u = v;
        }
    }

    public static bool IsNegative(double x) => x < (0 - Tolerance);

    private static bool IsNonZero(double x) => x < (0 - Tolerance) || (0 + Tolerance) < x;

    private static bool IsBoolean(double x) => x < (0 + Tolerance) || (1 - Tolerance) < x;
}
