using Gurobi;
using Kep.Runner;

namespace Kep.MultiStage;

/// <summary>
/// Represents the Cycle formulation of the KEP.
/// </summary>
public class CycleMultiFormulation(GRBEnv env, int k) : IMultiFormulation
{
    public const double Tolerance = 1e-6;

    public IEnumerable<(double objective, IReadOnlyCollection<int[]> cycles)> SolveMany(
        bool[,] hasArc, double[,] weights, int maxSolutions)
    {   
        var allCycles = CycleFormulation.GetAllCycles(hasArc, k).ToList();
        using var grbModel = CreateModel(maxSolutions, allCycles, weights);
        
        grbModel.Optimize();
        switch (grbModel.Status)
        {
            case GRB.Status.INFEASIBLE:
                throw new InvalidOperationException("infeasible");
            case GRB.Status.INF_OR_UNBD:
                throw new InvalidOperationException("infeasible or unbounded");
            case GRB.Status.OPTIMAL:
                break;
            default:
                throw new InvalidOperationException("something went wrong: " + grbModel.Status);
        }

        var vars = grbModel.GetVars();

        for (int i = 0; i < grbModel.SolCount; i++)
        {
            grbModel.Set(GRB.IntParam.SolutionNumber, i);
            
            var objective = -grbModel.PoolNObjVal;
            
            var xValues = grbModel.Get(GRB.DoubleAttr.PoolNX, vars);
            var selectedCycles = xValues
                .Zip(allCycles, (x, cycle) => (x, cycle))
                .Where(t => IsNonZero(t.x))
                .Select(t => t.cycle);
            
            yield return (objective, selectedCycles.ToList());
        }        
    }

    private GRBModel CreateModel(int numSolutions, List<int[]> cycles, double[,] arcWeights)
    {
        var problem = new GRBModel(env);
        problem.Set(GRB.IntParam.PoolSolutions, numSolutions);
        problem.Set(GRB.IntParam.PoolSearchMode, 2);
        
        var constraints = problem.AddConstrs(arcWeights.LengthI());
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
                objectiveCoefficient -= arcWeights[prevNode, node];
                cycleConstraints[i] = constraints[node];

                prevNode = node;
            }

            problem.AddVar(0, 1, objectiveCoefficient, GRB.BINARY, cycleConstraints.ToArray(), null, "c");
        }

        return problem;
    }

    private static bool IsNonZero(double x) => x < (0 - Tolerance) || (0 + Tolerance) < x;
}
