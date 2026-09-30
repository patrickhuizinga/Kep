using Gurobi;

namespace Kep.Runner;

/// <summary>
/// Represents the Cycle formulation of the KEP.
/// </summary>
public class CycleFormulation(int k) : GurobiFormulation
{
    protected override GRBModel CreateModel(GRBEnv env, bool[,] A, double[,] w)
    {
        var cycles = GetAllCycles(A, k);
        return CreateModel(env, cycles, w);
    }

    /// <summary>
    /// Creates a new GRBModel for the cycle formulation.
    /// </summary>
    /// <remarks>
    /// The formulation uses minimization over negative cycle weights. The final objective value will be negative.
    /// </remarks>
    public static GRBModel CreateModel(GRBEnv env, IEnumerable<int[]> cycles, double[,] arcWeights)
    {
        var problem = new GRBModel(env);
        
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

            problem.AddVar(0, 1, objectiveCoefficient, GRB.BINARY, cycleConstraints, null, "c");
        }
        
        return problem;
    }

    public static IEnumerable<int[]> GetAllCycles(bool[,] arcs, int k)
    {
        return Enumerable.Range(0, arcs.LengthI())
            .SelectMany(i => GetCyclesUpTo(arcs, [i], k - 1));
    }

    private static IEnumerable<int[]> GetCyclesUpTo(bool[,] arcs, int[] prev, int k)
    {
        var first = prev[0];
        var i = prev.Last();
        
        if (arcs[i, first])
            yield return prev;
        
        if (k == 0)
            yield break;

        // the lowest node in the cycle is always the first, to prevent duplications
        for (int j = first + 1; j < arcs.LengthJ(); j++)
        {
            if (!arcs[i, j]) continue;
            if (prev.Contains(j)) continue;

            var cycles = GetCyclesUpTo(arcs, [..prev, j], k - 1);
            foreach (var cycle in cycles)
                yield return cycle;
        }
    }
}
