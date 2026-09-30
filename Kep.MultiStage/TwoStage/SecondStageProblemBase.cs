using Gurobi;
using Kep.Runner.Problem;

namespace Kep.MultiStage.TwoStage;

public abstract class SecondStageProblemBase(double[,] weights)
{
    protected double[,] Weights { get; } = weights;

    public int CacheHits { get; private set; }
    public int CacheMisses { get; private set; }

    public abstract double CalculateExpectedObjective(
        (double objective, IReadOnlyCollection<int[]> cycles) prevSolution,
        Pool pool,
        GRBEnv env,
        CancellationToken cancellation);

    /// <summary>
    /// Returns the expected objective value for the given second stage solution.
    /// </summary>
    protected double ExpectedObjective(
        IReadOnlyCollection<int[]> cycles,
        Pool pool,
        int k,
        GRBEnv env,
        CancellationToken cancellation)
    {
        if (cancellation.IsCancellationRequested)
            return 0;
        
        var timedCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var combinedCancellation = CancellationTokenSource.CreateLinkedTokenSource(timedCancellation.Token, cancellation);

        // a 'pessimistic' arc is only `true` if it is definitely a match
        // this only includes the matches from solution(s) found in earlier stage(s)
        var arcs = pool.PessimisticCompatabilityMatrix;

        // enable every arc from the current solution to get the maximum possible cycles
        foreach (var cycle in cycles)
        foreach (var (u, v) in GetArcs(cycle))
            arcs[u, v] = true;

        var expectationFormulation = new ExpectationFormulation(Weights);
        var result = expectationFormulation.Run(arcs, k, pool, env, combinedCancellation.Token);

        CacheHits += expectationFormulation.CacheHits;
        CacheMisses += expectationFormulation.CacheMisses;
        
        return result;
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