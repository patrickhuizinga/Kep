using System.Runtime.InteropServices;
using Gurobi;
using Kep.Runner;
using Kep.MultiStage.Glorie2014;
using Kep.Runner.Problem;

namespace Kep.MultiStage.TwoStage;

public class ExpectationFormulation(double[,] weights)
{
    public const double Tolerance = 1e-6; 
    
    private readonly GraphMap<(double objective, List<uint> usedArcs)> _solutionCache = new();

    public double Run(
        bool[,] hasArc,
        int k,
        Pool pool,
        GRBEnv env,
        CancellationToken cancellation)
    {
        if (cancellation.IsCancellationRequested)
            return 0;

        return SplitDisjointSubgraphs(hasArc)
            .Sum(subgraph => RunSubgraph(subgraph, k, pool, env, cancellation));
    }

    public double Run(
        IReadOnlyCollection<int[]> cycles,
        Pool pool,
        GRBEnv env,
        CancellationToken cancellation)
    {
        if (cancellation.IsCancellationRequested)
            return 0;

        return SplitDisjointSubgraphs(cycles, pool.Size)
            .Sum(subgraph => RunSubgraph(subgraph, pool, env, cancellation));
        
    }
    
    private double RunSubgraph(
        bool[,] hasArc,
        int k,
        Pool pool,
        GRBEnv env,
        CancellationToken cancellation)
    {
        if (cancellation.IsCancellationRequested)
            return 0;

        var cycles = CycleFormulation.GetAllCycles(hasArc, k).ToList();
        
        if (ContainsAnyOverlap(cycles, pool.Size))
        {
            var arcs = GetBitArcs(hasArc).ToList();
            
            while (_prunedCyclesBuffers.Count < arcs.Count)
                _prunedCyclesBuffers.Add(new List<int[]>(cycles.Count));

            return RunSubgraphFork(arcs, cycles, pool, env, cancellation, 0);
        }
        
        return RunSimple(pool, cycles);
    }

    private double RunSubgraph(
        List<int[]> cycles,
        Pool pool,
        GRBEnv env,
        CancellationToken cancellation)
    {
        if (cancellation.IsCancellationRequested)
            return 0;
        
        var hasArc = new bool[pool.Size, pool.Size];
        
        foreach (var cycle in cycles)
        foreach (var (u, v) in GetArcs(cycle))
            hasArc[u,v] = true;

        if (ContainsAnyOverlap(cycles, pool.Size))
        {
            var arcs = GetBitArcs(hasArc).ToList();
            
            while (_prunedCyclesBuffers.Count < arcs.Count)
                _prunedCyclesBuffers.Add(new List<int[]>(cycles.Count));

            return RunSubgraphFork(arcs, cycles, pool, env, cancellation, 0);
        }
        
        return RunSimple(pool, cycles);
    }

    private double RunSimple(Pool pool, List<int[]> cycles)
    {
        var totalObjective = 0.0;
        foreach (var cycle in cycles)
        {
            var cycleObjective = GetCycleProbability(cycle, pool) * GetCycleWeight(cycle);
            totalObjective += cycleObjective;
        }

        return totalObjective;
    }

    private readonly List<List<int[]>> _prunedCyclesBuffers = [];
    
    private double RunSubgraphFork(
        List<uint> arcs,
        List<int[]> cycles,
        Pool pool,
        GRBEnv env,
        CancellationToken cancellation,
        int arcForkIndex)
    {
        if (cycles.Count == 0)
            return 0;
        
        // implicitly all later arcs are enabled
        var fullLeftSolution = GetOrSolve(cycles, env);

        if (arcForkIndex >= arcs.Count)
            return fullLeftSolution.objective;

        var leftUsedArcs = CollectionsMarshal.AsSpan(fullLeftSolution.usedArcs);
        
        int firstUnusedArcIndex = arcForkIndex;
        while (firstUnusedArcIndex < arcs.Count)
        {
            if (leftUsedArcs.Contains(arcs[firstUnusedArcIndex]))
                firstUnusedArcIndex++;
            else
                break;
        }
            
        // rearrange the later arcs so that all used arc are at the front
        for (int i = arcs.Count - 1; firstUnusedArcIndex <= i; )
        {
            if (leftUsedArcs.Contains(arcs[i]))
            {
                (arcs[firstUnusedArcIndex], arcs[i]) = (arcs[i], arcs[firstUnusedArcIndex]);
                    
                while (firstUnusedArcIndex <= i)
                {
                    firstUnusedArcIndex++;
                    if (!leftUsedArcs.Contains(arcs[firstUnusedArcIndex]))
                        break;
                }
            }
            else
            {
                i--;
            }
        }
            
        // if this and all later arcs are unused, no need to branch; disabling them will have no effect
        if (firstUnusedArcIndex <= arcForkIndex)
            return fullLeftSolution.objective;

        // the branch with the arc
        var leftObjective = RunSubgraphForkLeft(arcs, cycles, pool, env, cancellation, arcForkIndex + 1, fullLeftSolution.objective, firstUnusedArcIndex);
        
        var arc = arcs[arcForkIndex];
        var arcU = (int)(arc >> 16);
        var arcV = (int)(arc & 0xFFFF);
        var arcProbability = pool.GetCompatibility(arcU, arcV);

        // if the arc is guaranteed to be included, no need to explore the other branch
        if (arcProbability >= 1 - Tolerance)
            return leftObjective;

        if (cancellation.IsCancellationRequested)
            return arcProbability * leftObjective;

        var prunedCycles = TryPruneCycles(cycles, arcU, arcV, arcForkIndex);

        // if no cycle contains the arc (anymore), no need to explore the other branch
        // if no cycles were pruned, no need to explore the other branch
        if (prunedCycles == null)
            return leftObjective;
        
        // the branch without the arc
        var rightObjective = RunSubgraphFork(arcs, prunedCycles, pool, env, cancellation, arcForkIndex + 1);

        return arcProbability * leftObjective + (1 - arcProbability) * rightObjective;
    }

    private double RunSubgraphForkLeft(
        List<uint> arcs,
        List<int[]> cycles,
        Pool pool,
        GRBEnv env,
        CancellationToken cancellation,
        int arcForkIndex,
        double fullLeftObjective,
        int firstUnusedArcIndex)
    {
        // if this and all later arcs are unused, no need to branch; disabling them will have no effect
        if (firstUnusedArcIndex <= arcForkIndex)
            return fullLeftObjective;

        // the branch with the arc
        var leftObjective = RunSubgraphForkLeft(arcs, cycles, pool, env, cancellation, arcForkIndex + 1, fullLeftObjective, firstUnusedArcIndex);

        var arc = arcs[arcForkIndex];
        var arcU = (int)(arc >> 16);
        var arcV = (int)(arc & 0xFFFF);
        var arcProbability = pool.GetCompatibility(arcU, arcV);

        // if the arc is guaranteed to be included, no need to explore the other branch
        if (arcProbability >= 1 - Tolerance)
            return leftObjective;

        if (cancellation.IsCancellationRequested)
            return arcProbability * leftObjective;

        var prunedCycles = TryPruneCycles(cycles, arcU, arcV, arcForkIndex);

        // if no cycles were pruned, no need to explore the other branch
        if (prunedCycles == null)
            return leftObjective;
        
        // the branch without the arc
        var rightObjective = RunSubgraphFork(arcs, prunedCycles, pool, env, cancellation, arcForkIndex + 1);

        return arcProbability * leftObjective + (1 - arcProbability) * rightObjective;
    }

    private List<int[]>? TryPruneCycles(List<int[]> cycles, int u, int v, int arcForkIndex)
    {
        int i = 0;
        for (; i < cycles.Count; i++)
        {
            var cycle = cycles[i];
            var prevNode = cycle[^1];
            foreach (var currNode in cycle)
            {
                if (prevNode == u && currNode == v)
                    goto pruneFirstCycle;
                prevNode = currNode;
            }

            continue;

        pruneFirstCycle:
            break;
        }

        // if no cycle contained the arc, then there are no cycles to prune
        if (i == cycles.Count)
            return null;

        var prunedCycles = _prunedCyclesBuffers[arcForkIndex];
        prunedCycles.Clear();
        prunedCycles.Capacity = cycles.Count - 1;

        for (int j = 0; j < i; j++)
            prunedCycles.Add(cycles[j]);

        for (; i < cycles.Count; i++)
        {
            var cycle = cycles[i];
            var prevNode = cycle[^1];
            foreach (var currNode in cycle)
            {
                if (prevNode == u && currNode == v)
                    goto skipCycle;
                prevNode = currNode;
            }

            prunedCycles.Add(cycle);

        skipCycle: ;
        }

        return prunedCycles;
    }

    public int CacheHits { get; private set; }
    public int CacheMisses { get; private set; }

    private (double objective, List<uint> usedArcs) GetOrSolve(List<int[]> cycles, GRBEnv env)
    {
        switch (cycles.Count)
        {
            case 0:
            case 1:
            case 2:
            case 3:
            case 4:
                return BruteForceFormulation.Solve(cycles, weights);
            default:
                ref var cachedResult = ref _solutionCache.GetOrAddDefault(cycles);
                if (cachedResult.usedArcs != null)
                {
                    CacheHits++;
                    return cachedResult;
                }

                CacheMisses++;
                cachedResult = Solve(cycles, env);
                return cachedResult;
        }
    }

    private (double objective, List<uint> usedArcs) Solve(List<int[]> cycles, GRBEnv env)
    {
        var objective = 0.0;
        List<uint>? usedArcs = null;
        
        var n = weights.LengthI();
        foreach (var subgraph in SplitDisjointSubgraphs(cycles, n))
        {
            var result = SolveSubgraph(subgraph, env);
            objective += result.objective;
            if (usedArcs == null)
                usedArcs = result.usedArcs;
            else
                usedArcs.AddRange(result.usedArcs);
        }

        return (objective, usedArcs ?? []);
    }

    private (double objective, List<uint> usedArcs) SolveSubgraph(List<int[]> cycles, GRBEnv env)
    {
        if (cycles.Count > 20)
            return SolveMip(cycles, env);
        
        return BruteForceFormulation.Solve(cycles, weights);
    }

    private (double objective, List<uint> usedArcs) SolveMip(IReadOnlyCollection<int[]> cycles, GRBEnv env)
    {
        using var problem = CycleFormulation.CreateModel(env, cycles, weights);
        
        problem.Optimize();

        var objective = -problem.ObjVal;
        
        var usedArcs = problem.GetXValues()
            .Zip(cycles, (x,  cycle) => (x, cycle))
            .Where(col => col.x > Tolerance)
            .SelectMany(col => GetBitArcs(col.cycle))
            .ToList();

        return (objective, usedArcs);
    }

    private static bool ContainsAnyOverlap(IEnumerable<int[]> cycles, int size)
    {
        var nodeUsed = new bool[size];
        
        foreach (var cycle in cycles)
        foreach (var node in cycle)
        {
            if (nodeUsed[node])
                return true;

            nodeUsed[node] = true;
        }

        return false;
    }

    /// <returns>All disjoint subgraphs</returns>
    private static IEnumerable<bool[,]> SplitDisjointSubgraphs(bool[,] hasArc)
    {
        // return new List<bool[,]> { hasArc };
        var result = new List<bool[,]?> { null };
        
        var subgraphPerNode = new int[hasArc.LengthI()];
        foreach (var (u, v) in GetArcs(hasArc))
        {
            var subgraphUIndex = subgraphPerNode[u];
            var subgraphVIndex = subgraphPerNode[v];
            
            if (subgraphUIndex == 0)
            {
                if (subgraphVIndex == 0)
                {
                    bool[,] subgraph = new bool[hasArc.LengthI(), hasArc.LengthJ()];
                    subgraphPerNode[u] = result.Count;
                    subgraphPerNode[v] = result.Count;
                    result.Add(subgraph);
                    subgraph[u, v] = true;
                }
                else
                {
                    var subgraph = result[subgraphVIndex]!;
                    subgraphPerNode[u] = subgraphVIndex;
                    subgraph[u, v] = true;
                }
            }
            else
            {
                if (subgraphVIndex == 0)
                {
                    var subgraph = result[subgraphUIndex]!;
                    subgraphPerNode[v] = subgraphUIndex;
                    subgraph[u, v] = true;
                }
                else if (subgraphUIndex == subgraphVIndex)
                {
                    var subgraph = result[subgraphUIndex]!;
                    subgraph[u, v] = true;
                }
                else
                {
                    var subgraph = result[subgraphUIndex]!;
                    var oldSubgraph = result[subgraphVIndex]!;
                    for (int i = 0; i < subgraph.LengthI(); i++)
                    for (int j = 0; j < subgraph.LengthJ(); j++)
                    {
                        if (oldSubgraph[i, j])
                            subgraph[i, j] = true;
                    }

                    for (int i = 0; i < subgraphPerNode.Length; i++)
                    {
                        if (subgraphPerNode[i] == subgraphVIndex)
                            subgraphPerNode[i] = subgraphUIndex;
                    }

                    result[subgraphVIndex] = null;
                    
                    subgraph[u, v] = true;
                }
            }
        }

        return result.Where(g => g != null)!;
    }

    /// <returns>All disjoint subgraphs</returns>
    private static IEnumerable<List<int[]>> SplitDisjointSubgraphs(IReadOnlyCollection<int[]> cycles, int n)
    {
        // no more than n subgraphs
        var result = new List<List<int[]>>(n);
        
        var subgraphPerNode = new int[n];
        Array.Fill(subgraphPerNode, -1);

        foreach (var cycle in cycles)
        {
            var cycleSubgraphIndex = -1;
            
            foreach (var node in cycle)
            {
                var nodeSubgraphIndex = subgraphPerNode[node];

                if (cycleSubgraphIndex == nodeSubgraphIndex)
                    continue;

                if (nodeSubgraphIndex == -1)
                    continue;
                // else some other cycle already added this node to a subgraph
                
                if (cycleSubgraphIndex == -1)
                {
                    result[nodeSubgraphIndex].Add(cycle);
                    cycleSubgraphIndex = nodeSubgraphIndex;
                    continue;
                }
                // else we already added this cycle to a different subgraph
                // therefor merge those subgraphs

                foreach (var c in result[cycleSubgraphIndex])
                {
                    result[nodeSubgraphIndex].Add(c);
                }
                // result[nodeSubgraphIndex].AddRange(result[cycleSubgraphIndex]);
                result[cycleSubgraphIndex].Clear();
                
                for (int i = 0; i < subgraphPerNode.Length; i++)
                {
                    if (subgraphPerNode[i] == cycleSubgraphIndex)
                        subgraphPerNode[i] = nodeSubgraphIndex;
                }

                cycleSubgraphIndex = nodeSubgraphIndex;
            }

            if (cycleSubgraphIndex == -1)
            {
                cycleSubgraphIndex = result.Count;
                // no more cycles per subgraph than the total number of subgraphs
                result.Add(new List<int[]>(cycles.Count) { cycle });
            }

            foreach (var node in cycle)
                subgraphPerNode[node] = cycleSubgraphIndex;
        }

        return result.Where(subgraph => subgraph.Count > 0);
    }

    private static IEnumerable<(int i, int j)> GetArcs(bool[,] hasArc)
    {
        return hasArc.Indices();
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

    private static IEnumerable<uint> GetBitArcs(bool[,] hasArc)
    {
        var (lengthI, lengthJ) = hasArc.Dim();

        for (int u = 0; u < lengthI; u++)
        for (int v = 0; v < lengthJ; v++)
        {
            if (hasArc[u, v])
                yield return (uint)u << 16 | (uint)v;
        }
    }

    private double GetCycleWeight(int[] cycle)
    {
        if (cycle.Length == 0)
            return 0.0;
        
        var result = 0.0;

        var w = weights;
        var prevNode = cycle[^1];
        foreach (var node in cycle)
        {
            result += w[prevNode, node];
            
            prevNode = node;
        }

        return result;
    }

    private static double GetCycleProbability(int[] cycle, Pool pool)
    {
        var probability = 1.0;

        var prevNode = cycle[^1];
        foreach (var node in cycle)
        {
            probability *= pool.GetCompatibility(prevNode, node);
            
            prevNode = node;
        }

        return probability;
    }

    private static IEnumerable<uint> GetBitArcs(int[] cycle)
    {
        if (cycle.Length == 0)
            yield break;
        
        var u = cycle[^1];
        foreach (var v in cycle)
        {
            yield return (uint)u << 16 | (uint)v;
            u = v;
        }
    }
}
