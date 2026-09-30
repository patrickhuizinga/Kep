using Gurobi;
using Kep.Runner;
using Kep.Runner.Problem;

namespace Kep.MultiStage.TwoStage;

public class RandomSecondStageProblem(double[,] weights, int k, int attempts) : SecondStageProblemBase(weights)
{   
    public override double CalculateExpectedObjective(
        (double objective, IReadOnlyCollection<int[]> cycles) prevSolution,
        Pool pool,
        GRBEnv env,
        CancellationToken cancellation)
    {
        var allCycles = CycleFormulation.GetAllCycles(pool.OptimisticCompatabilityMatrix, k).ToArray();
        
        return Enumerable.Range(0, attempts)
            .Select(i =>
            {
                var rng = new Random(i);
                rng.Shuffle(allCycles);
                
                var cycles = MakeCycleSelection(pool, allCycles);

                return ExpectedObjective(cycles, pool, k, env, cancellation);
            }).Max();
    }

    private static List<int[]> MakeCycleSelection(Pool pool, int[][] allCycles)
    {
        var cycles = new List<int[]>();
        var coveredNodes = new bool[pool.Size];
                
        foreach (var cycle in allCycles)
        {
            if (HasOverlap(cycle, coveredNodes))
                continue;
        
            cycles.Add(cycle);
        
            foreach (var node in cycle)
                coveredNodes[node] = true;
        }

        return cycles;
    }

    private static bool HasOverlap(int[] cycle, bool[] coveredNodes)
    {
        return cycle.Any(node => coveredNodes[node]);
    }
}
