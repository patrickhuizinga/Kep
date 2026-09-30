namespace Kep.MultiStage.TwoStage;

public static class BruteForceFormulation
{
    public static (double objective, List<uint> usedArcs) Solve(List<int[]> cycles, double[,] arcWeights)
    {
        switch (cycles.Count)
        {
            case 0:
                return (0.0, []);
            case 1:
                return SolveSingle(cycles[0], arcWeights);
            case 2:
                return SolveDouble(cycles[0], cycles[1], arcWeights);
            default:
                return SolveWithBranching(cycles, arcWeights);
        }
    }

    private static (double objective, List<uint> usedArcs) SolveSingle(int[] cycle, double[,] arcWeights)
    {
        var objective = 0.0;
        var bitArcs = new List<uint>(cycle.Length);
        
        var u = cycle[^1];
        foreach (var v in cycle)
        {
            objective += arcWeights[u, v];
            
            var bitArc = (uint)u << 16 | (uint)v;
            bitArcs.Add(bitArc);
            
            u = v;
        }

        return (objective, bitArcs);
    }

    private static (double objective, List<uint> usedArcs) SolveDouble(
        int[] cycle1, int[] cycle2, double[,] arcWeights)
    {
        return Overlaps(cycle1, cycle2)
            ? SolveDoubleOverlap(cycle1, cycle2, arcWeights)
            : SolveDoubleDisjoint(cycle1, cycle2, arcWeights);
    }

    private static (double objective, List<uint> usedArcs) SolveDoubleOverlap(int[] cycle1, int[] cycle2, double[,] arcWeights)
    {
        var objective1 = GetCycleWeight(cycle1, arcWeights);
        var objective2 = GetCycleWeight(cycle2, arcWeights);

        if (objective1 > objective2)
        {
            var arcs = new List<uint>(cycle1.Length);
            AddArcs(cycle1, arcs);

            return (objective1, arcs);
        }
        else
        {
            var arcs = new List<uint>(cycle2.Length);
            AddArcs(cycle2, arcs);

            return (objective2, arcs);
        }
    }

    private static (double objective, List<uint> usedArcs) SolveDoubleDisjoint(int[] cycle1, int[] cycle2, double[,] arcWeights)
    {
        var objective = GetCycleWeight(cycle1, arcWeights) + GetCycleWeight(cycle2, arcWeights);
        var arcs = new List<uint>(cycle1.Length + cycle2.Length);

        AddArcs(cycle1, arcs);
        AddArcs(cycle2, arcs);

        return (objective, arcs);
    }

    private static (double objective, List<uint> usedArcs) SolveWithBranching(List<int[]> cycles, double[,] arcWeights)
    {
        var cycleWeights = new double[cycles.Count];
        
        var cycleOverlaps = new uint[cycles.Count];
        for (int i = 0; i < cycles.Count; i++)
        {
            for (int j = i + 1; j < cycles.Count; j++)
            {
                var overlap = Overlaps(cycles[i], cycles[j]);
                
                if (overlap)
                {
                    cycleOverlaps[i] |= 1u << j;
                    cycleOverlaps[j] |= 1u << i;
                }
            }
            
            cycleWeights[i] = GetCycleWeight(cycles[i], arcWeights);
        }
        
        var result = SolveWithBranchingFork(
            cycleWeights, uint.MaxValue, cycleOverlaps, 0);

        var usedArcs = GetUsedArcs(cycles, result.selectedCycles);
        return (result.objective, usedArcs);
    }

    private static (double objective, uint selectedCycles) SolveWithBranchingFork(
        double[] cycleWeights, uint selectedCycles, uint[] cycleOverlaps, int cycleForkingIndex)
    {
        var n = cycleWeights.Length;
        if (cycleForkingIndex >= n)
        {
            double objective = 0;
            for (var i = 0; i < cycleWeights.Length; i++)
            {
                if ((selectedCycles & (1u << i)) != 0)
                    objective += cycleWeights[i];
            }

            return (objective, selectedCycles);
        }

        var cycleOverlap = cycleOverlaps[cycleForkingIndex] & selectedCycles;

        var earlierOverlap = cycleOverlap & ~(uint.MaxValue << cycleForkingIndex);
        if (earlierOverlap != 0)
        {
            // if we overlap with an earlier cycle, we can't use this cycle
            // so solve it without this cycle
        
            selectedCycles &= ~(1u << cycleForkingIndex);

            return SolveWithBranchingFork(
                cycleWeights, selectedCycles, cycleOverlaps, cycleForkingIndex + 1);
        }

        var resultLeft = SolveWithBranchingFork(
            cycleWeights, selectedCycles, cycleOverlaps, cycleForkingIndex + 1);

        // if we don't overlap with any later cycle, there is no need to disable this cycle
        var laterOverlap = cycleOverlap & (uint.MaxValue << cycleForkingIndex);
        if (laterOverlap == 0)
            return resultLeft;

        // solve it without this cycle
        selectedCycles &= ~(1u << cycleForkingIndex);

        var resultRight = SolveWithBranchingFork(
            cycleWeights, selectedCycles, cycleOverlaps, cycleForkingIndex + 1);

        if (resultLeft.objective > resultRight.objective)
            return resultLeft;

        return resultRight;
    }

    private static bool Overlaps(int[] cycle1, int[] cycle2)
    {
        foreach (var node1 in cycle1)
        foreach (var node2 in cycle2)
        {
            if (node1 == node2)
                return true;
        }

        return false;
    }

    private static double GetCycleWeight(int[] cycle, double[,] arcWeights)
    {
        var result = 0.0;

        var u = cycle[^1];
        foreach (var v in cycle)
        {
            result += arcWeights[u, v];
            
            u = v;
        }

        return result;
    }

    private static List<uint> GetUsedArcs(List<int[]> cycles, uint selectedCycles)
    {
        int totalArcCount = 0;
        for (var i = 0; i < cycles.Count; i++)
        {
            if ((selectedCycles & (1u << i)) != 0)
                totalArcCount += cycles[i].Length;
        }
        
        var usedArcs = new List<uint>(totalArcCount);
        for (var i = 0; i < cycles.Count; i++)
        {
            if ((selectedCycles & (1u << i)) == 0)
                continue;
            
            var cycle = cycles[i];
            
            AddArcs(cycle, usedArcs);
        }

        return usedArcs;
    }

    private static void AddArcs(int[] cycle, List<uint> arcs)
    {
        var u = cycle[^1];
        foreach (var v in cycle)
        {
            var bitArc = (uint)u << 16 | (uint)v;
            arcs.Add(bitArc);
            u = v;
        }
    }
}
