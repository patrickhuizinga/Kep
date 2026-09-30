using Gurobi;
using Kep.Runner;
using Kep.Runner.Problem;

namespace Kep.MultiStage.TwoStage;

public class WeightedRandomSecondStageProblem(double[,] weights, int k, int attempts, int seed) : SecondStageProblemBase(weights)
{   
    private readonly Random _rng = new(seed);

    public override double CalculateExpectedObjective(
        (double objective, IReadOnlyCollection<int[]> cycles) prevSolution,
        Pool pool,
        GRBEnv env,
        CancellationToken cancellation)
    {
        // mark all nodes that were in the previous solution
        var nodeBonus = new double[pool.Size];
        foreach (var cycle in prevSolution.cycles)
        foreach (var node in cycle)
            nodeBonus[node] = 1;
        
        var allCycles = CycleFormulation.GetAllCycles(pool.OptimisticCompatabilityMatrix, k).ToArray();
        
        var cycleFavor = new double[allCycles.Length];
        for (int i = 0; i < allCycles.Length; i++)
        {
            var cycle = allCycles[i];
            
            double cycleFav = 1;
            
            var u = cycle[^1];
            foreach (var v in cycle)
            {
                // favor a cycle more for every node in the previous solution it contains
                cycleFav += nodeBonus[v];
                
                // favor a cycle more if the arcs are known to be compatible
                if (pool.KnownMatches[u, v] == true)
                    cycleFav += 1;

                u = v;
            }
            
            cycleFavor[i] = cycleFav;
        }
        
        // strongly prefer cycles that haven't been tried before
        var cycleNeverBonus = new double[allCycles.Length];
        Array.Fill(cycleNeverBonus, 10);
        
        List<double> objectiveValues = new(capacity: attempts);

        for (int i = 0; i < attempts; i++)
        {
            var cycles = new List<int[]>();
            var cycleIndices = new List<int>();
            var coveredNodes = new bool[pool.Size];
            
            var totalFavor = cycleFavor.Sum() + cycleNeverBonus.Sum();

            // try up to n cycles
            for (int j = 0; j < pool.Size * 2; j++)
            {
                var favorCutoff = _rng.NextDouble() * totalFavor;
                
                int l = 0;
                for (; l < allCycles.Length; l++)
                {
                    favorCutoff -= cycleFavor[l] + cycleNeverBonus[l];
                    
                    if (favorCutoff <= 0)
                        break;
                }
                
                if (l == allCycles.Length)
                    continue;
                
                var cycle = allCycles[l];
                if (HasOverlap(cycle, coveredNodes))
                    continue;

                cycles.Add(cycle);
                cycleIndices.Add(l);

                foreach (var node in cycle)
                    coveredNodes[node] = true;

                cycleNeverBonus[l] = 0;
            }

            var expectation = ExpectedObjective(cycles, pool, k, env, cancellation);

            var index = objectiveValues.BinarySearch(expectation);
            if (index < 0)
                index = ~index;
            
            var favorInc = (double)objectiveValues.Count/index;
            objectiveValues.Insert(index, expectation);

            // if there are not enough samples to have a proper understanding of 'good' and 'bad' results
            if (objectiveValues.Count < pool.Size)
                continue;

            if (favorInc < 0.25)
            {
                foreach (var cycleIndex in cycleIndices)
                {
                    cycleFavor[cycleIndex] -= 0.2;
                    if (cycleFavor[cycleIndex] < 0.1)
                        cycleFavor[cycleIndex] = 0.1;
                }
            } else if (favorInc < 0.5)
            {
                continue;
            } else if (favorInc < 0.75)
            {
                favorInc = 0.1;
            } else if (favorInc < 0.9)
            {
                favorInc = 0.2;
            }
            else
            {
                favorInc = 0.3;
            }

            foreach (var cycleIndex in cycleIndices)
                cycleFavor[cycleIndex] += favorInc;
        }

        return objectiveValues[^1];
    }

    private static bool HasOverlap(int[] cycle, bool[] coveredNodes)
    {
        return cycle.Any(node => coveredNodes[node]);
    }
}
