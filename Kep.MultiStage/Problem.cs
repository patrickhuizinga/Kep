using Gurobi;
using Kep.MultiStage.TwoStage;
using Kep.Runner.Problem;

namespace Kep.MultiStage;

public class Problem(
    double[,] weights, int k,
    int maxFirstStageSolutions, int firstStageScenarios,
    SecondStageProblemBase secondStageProblem)
{
    public double Solve(Pool pool, Random rng, CancellationToken cancellation)
    {
        using var env = new GRBEnv();
        env.LogToConsole = 0;
        env.OutputFlag = 0;
        env.Threads = 1;
        env.Start();

        // var formulation = new CycleProblem3(env, k);
        var formulation = new IcsMultiFormulation(env, k, 0.99);
        var arcs = pool.OptimisticCompatabilityMatrix;
        var firstSolutions = formulation.SolveMany(arcs, weights, maxFirstStageSolutions).ToList();
        var expectedList = new List<double>(maxFirstStageSolutions);
        
        var best = firstSolutions
            .Select((solution, i) =>
            {
                Console.WriteLine($"solution {i,2}");
                
                var expectationFormulation = new ExpectationFormulation(weights);
                var expectedObjective = expectationFormulation.Run(solution.cycles, pool, env, CancellationToken.None);
                Console.WriteLine($"MIP {solution.objective,2}     expected: {expectedObjective,5:F2}");
                
                expectedList.Add(expectedObjective);
                return EvaluateFirstSolution(solution, pool, env, cancellation, rng);
            })
            .Max();
        Console.WriteLine("mean expected: " + expectedList.Average());
        Console.WriteLine();
        return best;
    }

    private double EvaluateFirstSolution(
        (double objective, IReadOnlyCollection<int[]> cycles) solution,
        Pool unobservedPool,
        GRBEnv env,
        CancellationToken cancellation,
        Random rng)
    {
        var finalMean = Enumerable.Range(0, firstStageScenarios)
            .Select(_ => EvaluateFirstSolutionOnce(solution, unobservedPool, env, cancellation, rng))
            .Average();

        Console.WriteLine($"                avg: {finalMean,6:F3}");

        Console.WriteLine();
        
        return finalMean;
    }

    private double EvaluateFirstSolutionOnce(
        (double objective, IReadOnlyCollection<int[]> cycles) solution,
        Pool unobservedPool,
        GRBEnv env,
        CancellationToken cancellation,
        Random rng)
    {
        if (cancellation.IsCancellationRequested)
            return 0;

        var partiallyObservedPool = unobservedPool.Clone();
        foreach (var cycle in solution.cycles)
        foreach (var (u, v) in GetArcs(cycle))
            partiallyObservedPool.GetOrDrawIsMatch(u, v, rng);
        
        var timedCancellation = new CancellationTokenSource(TimeSpan.FromMinutes(1));
        var combinedCancellation = CancellationTokenSource.CreateLinkedTokenSource(timedCancellation.Token, cancellation);

        var expectedObjective = secondStageProblem.CalculateExpectedObjective(solution, partiallyObservedPool, env, combinedCancellation.Token);
        Console.WriteLine($"  scenario expected: {expectedObjective,6:F3}");
        
        return expectedObjective;
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
}
