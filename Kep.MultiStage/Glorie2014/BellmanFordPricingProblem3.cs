namespace Kep.MultiStage.Glorie2014;

public class BellmanFordPricingProblem3(bool[,] hasArc, double[,] arcWeights, int k) : IPricingProblem
{
    public IEnumerable<int[]> SolveMany(double[] duals)
    {
        if (k < 2)
            yield break;
        
        var arcDistRev = new double[duals.Length][];
        for (int v = 0; v < duals.Length; v++)
        {
            arcDistRev[v] = new double[duals.Length];
            for (int u = 0; u < duals.Length; u++)
            {
                if (hasArc[u, v])
                    arcDistRev[v][u] = -arcWeights[u, v] - duals[v];
                else
                    arcDistRev[v][u] = double.PositiveInfinity;
            }
        }

        // modified Bellman-Ford algorithm
        // Bellman-Ford tries to deal with (prevent) negative distance cycles, but we actually want them.
        // We simply run BF k times and then look for any i-to-i path that has a negative distance.
        // Each such path contains at least one cycle and might contain a valid column.
        // With k large enough, we might have actually traversed multiple cycles (or the same multiple times),
        // so we need to make sure we don't return invalid cycles
        
        double[] distances;

        // due to negative cycles, we must remember all predecessors of previous generations,
        // or we might end up with two or more nodes pointing to each other for the cheapest route to get to a third,
        int[][] predecessor;
        
        for (int source = 0; source < duals.Length; source++)
        {
            distances = new double[duals.Length];
            predecessor = new int[k][];
            for (int i = 0; i < k; i++)
                predecessor[i] = new int[duals.Length];

            ComputeDistancesFirst(source);
            ComputeDistancesMid(source);
            ComputeDistancesLast(source);
            var t = GetCycle(source);
            if (MasterProblem.IsNegative(t.cost))
                yield return t.cycle;
        }

        yield break;

        void ComputeDistancesFirst(int source)
        {
            // initialize distances and predecessors
            
            // iteration = 0
            for (int v = 0; v <= source; v++)
            {
                distances[v] = arcDistRev[v][source];
                predecessor[0][v] = v;
            }
        }

        void ComputeDistancesMid(int source)
        {
            for (int iteration = 1; iteration < k - 1; iteration++)
            {
                var prevDist = distances;
                var nextDist = distances = new double[duals.Length];
                var prevPred = predecessor[iteration - 1];
                var nextPred = predecessor[iteration];

                // a cycle never goes to a higher node than where it started
                // such a cycle should (and will) start at the highest node
                for (int v = 0; v <= source; v++)
                {
                    var dist = prevDist[v];
                    var pred = prevPred[v];
                    var arcDist = arcDistRev[v];

                    // other than the start/end node, every node in the cycle is strictly lower
                    for (int u = 0; u < source; u++)
                    {
                        var newDistance = prevDist[u] + arcDist[u];

                        if (dist > newDistance)
                        {
                            dist = newDistance;
                            pred = u;
                        }
                    }

                    nextDist[v] = dist;
                    nextPred[v] = pred;
                }
            }
        }

        void ComputeDistancesLast(int source)
        {
            // in the last iteration we only care about returning to source
            
            // iteration = k-1
            var prevDist = distances;
            // v = source
            var dist = prevDist[source];
            var pred = predecessor[k - 2][source];
            var arcDist = arcDistRev[source];

            // other than the start/end node, every node in the cycle is strictly lower
            for (int u = 0; u < source; u++)
            {
                var newDistance = prevDist[u] + arcDist[u];

                if (dist > newDistance)
                {
                    dist = newDistance;
                    pred = u;
                }
            }

            distances[source] = dist;
            predecessor[k - 1][source] = pred;
        }

        (int[] cycle, double cost) GetCycle(int source)
        {
            if (!MasterProblem.IsNegative(distances[source]))
                return ([], distances[source]);
            
            var cycle = new List<int>(k);
            var currentNode = source;
            for (int iteration = k - 1; iteration >= 0; iteration--)
            {
                currentNode = predecessor[iteration][currentNode];

                // we're back at the first node; the cycle is complete
                if (currentNode == source)
                    break;

                for (int i = 0; i < cycle.Count; i++)
                {
                    // we revisit some earlier node; a cycle in our cycle, a 'figure 8'
                    // remove that cycle.
                    if (cycle[i] == currentNode)
                        cycle.RemoveRange(i, cycle.Count - i);
                }
                
                cycle.Add(currentNode);
            }
            
            cycle.Add(source);
            cycle.Reverse();
            
            // in the previous step we might have fixed a 'figure 8'
            // so compute the cost of the actual cycle
            var actualCost = GetCost(cycle);
            return (cycle.ToArray(), actualCost);
        }

        double GetCost(IReadOnlyList<int> cycle)
        {
            // min c'x ; Ax <= b
            // rc = c - A'y
            // rc_j = c_j - sum_i A_ij y_i
            
            double cost = 0;
            var prevNode = cycle[^1];
            foreach (var node in cycle)
            {
                cost += arcDistRev[node][prevNode];
                
                prevNode = node;
            }
            
            return cost;
        }
    }
}
