using Gurobi;
using Kep.Runner;

namespace Kep.MultiStage;

/// <summary>
/// Represents the Cycle formulation of the KEP.
/// </summary>
public class IcsMultiFormulation(GRBEnv env, int k, double alpha) : IMultiFormulation
{
    public const double Tolerance = 1e-6;

    public IEnumerable<(double objective, IReadOnlyCollection<int[]> cycles)> SolveMany(
        bool[,] hasArc, double[,] arcWeights, int maxSolutions)
    {   
        var allCycles = CycleFormulation.GetAllCycles(hasArc, k).ToList();
        var cycleWeights = allCycles.Select(cycle => GetCycleWeight(cycle, arcWeights)).ToList();
        var n = arcWeights.LengthI();

        using var grbModel = CreateModel(allCycles, cycleWeights, n);

        for (int solIndex = 0; solIndex < maxSolutions; solIndex++)
        {
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

            var objective = grbModel.ObjVal;
            var vars = grbModel.GetVars();
            var xValues = grbModel.Get(GRB.DoubleAttr.X, vars);
            
            var selectedCycles = new List<int[]>();
            for (int i = 0; i < xValues.Length; i++)
            {
                if (IsNonZero(xValues[i]))
                    selectedCycles.Add(allCycles[i]);
            }
            
            // todo deal with duplicate solutions
            yield return (objective, selectedCycles);
            
            for (int i = 0; i < xValues.Length; i++)
            {
                if (IsNonZero(xValues[i]))
                {
                    // make cycle worth less for next iteration
                    vars[i].Obj *= alpha;
                }
            }
        }      
    }

    private GRBModel CreateModel(List<int[]> cycles, List<double> cycleWeights, int n)
    {
        var problem = new GRBModel(env);
        
        var constraints = problem.AddConstrs(n);
        foreach (var constr in constraints)
        {
            constr.Sense = GRB.LESS_EQUAL;
            constr.RHS = 1;
        }

        for (var i = 0; i < cycles.Count; i++)
        {
            var objectiveCoefficient = cycleWeights[i];
         
            var cycle = cycles[i];   
            var cycleConstraints = new GRBConstr[cycle.Length];
            for (var j = 0; j < cycle.Length; j++)
            {
                var node = cycle[j];
                cycleConstraints[j] = constraints[node];
            }

            problem.AddVar(0, 1, objectiveCoefficient, GRB.BINARY, cycleConstraints, null, "c");
        }

        problem.ModelSense = GRB.MAXIMIZE;
        return problem;
    }

    private static double GetCycleWeight(int[] cycle, double[,] arcWeights)
    {
        var result = 0.0;

        var u = cycle.Last();
        foreach (var v in cycle)
        {
            result += arcWeights[u, v];
            u = v;
        }

        return result;
    }

    private static bool IsNonZero(double x) => x < (0 - Tolerance) || (0 + Tolerance) < x;
}
