using Kep.Runner;

namespace Kep.MultiStage.Glorie2014;

public class DfsPricingProblem(bool[,] hasArc, double[,] arcWeights, int k) : IPricingProblem
{
    public IEnumerable<int[]> SolveMany(double[] duals)
    {
        for (int i = 0; i < hasArc.LengthI(); i++)
        {
            (int[] cycle, double price) cheapest = ([], 0);
            
            CheapestCycle(duals, [i], 0, ref cheapest);
            
            if (MasterProblem.IsNegative(cheapest.price))
                yield return cheapest.cycle;
        }
    }

    private void CheapestCycle(
        double[] duals, List<int> currentPath, double currentPathPrice,
        ref (int[] cycle, double price) cheapest)
    {
        int startNode = currentPath[0];
        int currentNode = currentPath[^1];

        if (hasArc[currentNode, startNode])
        {
            var arcCost = -arcWeights[currentNode, startNode] - duals[startNode];
            var nextPrice = currentPathPrice + arcCost;

            if (nextPrice < cheapest.price)
                cheapest = (currentPath.ToArray(), nextPrice);
        }

        if (currentPath.Count == k)
            return;
        
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
            CheapestCycle(duals, currentPath, nextPrice, ref cheapest);
        }
        
        currentPath.RemoveAt(currentPath.Count - 1);
    }
}
