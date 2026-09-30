using Kep.Runner;

namespace Kep.MultiStage.Glorie2014;

public class SimplisticPricingProblem : IPricingProblem
{
    private readonly List<int[]> _cycles;
    private readonly double[,] _arcWeights;

    public SimplisticPricingProblem(List<int[]> cycles, double[,] arcWeights)
    {
        _cycles = cycles;
        _arcWeights = arcWeights;
    }

    public SimplisticPricingProblem(bool[,] hasArc, int k, double[,] arcWeights)
    {
        _arcWeights = arcWeights;
        _cycles = GetCycles(hasArc, k).ToList();
    }
    
    public IEnumerable<int[]> SolveMany(double[] duals)
    {
        var tuple = _cycles
            .Select(cycle => (cycle, cost: GetCost(cycle)))
            .MinBy(x => x.cost);

        if (MasterProblem.IsNegative(tuple.cost))
            yield return tuple.cycle;
        
        yield break;
        
        double GetCost(IReadOnlyList<int> cycle)
        {
            // min c'x ; Ax <= b
            // rc = c - A'y
            // rc_j = c_j - sum_i A_ij y_i

            double cost = 0;
            var prevNode = cycle[^1];
            foreach (var node in cycle)
            {
                cost += -_arcWeights[prevNode, node] - duals[node];

                prevNode = node;
            }

            return cost;
        }
    }

    private static IEnumerable<int[]> GetCycles(bool[,] arcs, int k)
    {
        return Enumerable.Range(0, arcs.LengthI())
            .SelectMany(i => GetCyclesUpTo(arcs, [i], k - 1));
    }

    private static IEnumerable<int[]> GetCyclesUpTo(bool[,] arcs, int[] prev, int k)
    {
        var first = prev[0];
        var i = prev[^1];

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
