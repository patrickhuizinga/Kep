using Kep.Runner;

namespace Kep.MultiStage.Glorie2014;

public class DfsEarlyExitPricingProblem(bool[,] hasArc, double[,] arcWeights, int k) : IPricingProblem
{
    public IEnumerable<int[]> SolveMany(double[] duals)
    {
        for (int i = 0; i < hasArc.LengthI(); i++)
        {
            var cycle = FirstCheapCycle(duals, [i], 0);
            if (cycle != null)
                yield return cycle;
        }
    }

    private int[]? FirstCheapCycle(
        double[] duals, List<int> currentPath, double currentPathPrice)
    {
        int startNode = currentPath[0];
        int currentNode = currentPath[^1];

        if (hasArc[currentNode, startNode])
        {
            var arcCost = -arcWeights[currentNode, startNode] - duals[startNode];
            var nextPrice = currentPathPrice + arcCost;

            if (MasterProblem.IsNegative(nextPrice))
                return currentPath.ToArray();
        }

        if (currentPath.Count == k)
            return null;
        
        currentPath.Add(-1);
        for (int j = startNode + 1; j < hasArc.LengthJ(); j++)
        {
            if (!hasArc[currentNode, j])
                continue;
            if (currentPath.Contains(j))
                continue;

            var arcCost = -arcWeights[currentNode, j] - duals[j];
            var nextPrice = currentPathPrice + arcCost;

            currentPath[^1] = j;
            var cycle = FirstCheapCycle(duals, currentPath, nextPrice);
            if (cycle != null)
                return cycle;
        }
        
        currentPath.RemoveAt(currentPath.Count - 1);
        return null;
    }
}
