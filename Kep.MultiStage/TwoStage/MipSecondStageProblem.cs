using Gurobi;
using Kep.Runner;
using Kep.Runner.Problem;

namespace Kep.MultiStage.TwoStage;

public class MipSecondStageProblem(double[,] weights, int k2, int k3, int maxSolutions) : SecondStageProblemBase(weights)
{
    public const double DisappointmentCost = 0.1;
    public const double TestCost = 0.1;

    public override double CalculateExpectedObjective(
        (double objective, IReadOnlyCollection<int[]> cycles) prevSolution,
        Pool pool,
        GRBEnv env,
        CancellationToken cancellation)
    {
        // modified weights to prefer the same patients and arcs, to optimize for disappointment and testing cost
        
        // the certain arcs are all the arcs that have been tested 'negatively'; we know these arcs are compatible
        var certainArcs = pool.PessimisticCompatabilityMatrix;
        var modWeights = Weights + TestCost*certainArcs;

        // make picking the same patient again more likely
        // note: the nodes of all cycles are distinct
        foreach (var cycle in prevSolution.cycles)
        foreach (var u in cycle)
            for (int v = 0; v < modWeights.LengthJ(); v++)
                modWeights[u, v] += DisappointmentCost;
        
        var formulation = new CycleMultiFormulation(env, k2);
        // var formulation = new MultiCycleProblem(env, k2, 0.99);
        var arcs = pool.OptimisticCompatabilityMatrix;
        
        var secondSolutions = formulation.SolveMany(arcs, modWeights, maxSolutions);

        return secondSolutions
            .Select(sol => ExpectedObjective(sol.cycles, pool, k3, env, cancellation))
            .Max();
    }
}

public class MipIntermediateStageProblem(
    double[,] weights, int k, int maxSolutions, int scenarios,
    SecondStageProblemBase nextStageProblem, Random rng)
    : SecondStageProblemBase(weights)
{
    public const double TestCost = 0.1;

    public override double CalculateExpectedObjective(
        (double objective, IReadOnlyCollection<int[]> cycles) prevSolution,
        Pool pool,
        GRBEnv env,
        CancellationToken cancellation)
    {
        // modified weights to prefer the same arcs, to optimize for testing cost
        
        // the certain arcs are all the arcs that have been tested 'negatively'; we know these arcs are compatible
        var certainArcs = pool.PessimisticCompatabilityMatrix;
        var modWeights = Weights + TestCost*certainArcs;

        var formulation = new CycleMultiFormulation(env, k);
        // var formulation = new MultiCycleProblem(env, k, 0.99);
        var arcs = pool.OptimisticCompatabilityMatrix;
        
        var secondSolutions = formulation.SolveMany(arcs, modWeights, maxSolutions);

        return secondSolutions
            .Select(solution => EvaluateSolution(solution, pool, env, cancellation))
            .Max();
    }

    private double EvaluateSolution(
        (double objective, IReadOnlyCollection<int[]> cycles) solution,
        Pool unobservedPool,
        GRBEnv env,
        CancellationToken cancellation)
    {
        var finalMean = Enumerable.Range(0, scenarios)
            .Select(_ => EvaluateSolutionOnce(solution, unobservedPool, env, cancellation))
            .Average();

        return finalMean;
    }

    private double EvaluateSolutionOnce(
        (double objective, IReadOnlyCollection<int[]> cycles) solution,
        Pool unobservedPool,
        GRBEnv env,
        CancellationToken cancellation)
    {
        if (cancellation.IsCancellationRequested)
            return 0;

        var partiallyObservedPool = unobservedPool.Clone();
        foreach (var cycle in solution.cycles)
        foreach (var (u, v) in GetArcs(cycle))
            partiallyObservedPool.GetOrDrawIsMatch(u, v, rng);
        
        var timedCancellation = new CancellationTokenSource(TimeSpan.FromMinutes(1));
        var combinedCancellation = CancellationTokenSource.CreateLinkedTokenSource(timedCancellation.Token, cancellation);

        var expectedObjective = nextStageProblem.CalculateExpectedObjective(solution, partiallyObservedPool, env, combinedCancellation.Token);
        
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
