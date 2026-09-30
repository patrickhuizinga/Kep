namespace Kep.MultiStage.Glorie2014;

public class BellmanFordPricingProblem2(bool[,] hasArc, double[,] arcWeights, int k) : IPricingProblem
{
    public IEnumerable<int[]> SolveMany(double[] duals)
    {
        if (k < 1)
            return [];
        
        var arcDistances = new double[duals.Length, duals.Length];
        for (int u = 0; u < duals.Length; u++)
        for (int v = 0; v < duals.Length; v++)
        {
            arcDistances[u, v] = -arcWeights[u, v] - duals[v];
        }

        // modified Bellman-Ford algorithm
        // Bellman-Ford tries to deal with (prevent) negative distance cycles, but we actually want them.
        // We simply run BF k times and then look for any i-to-i path that has a negative distance.
        // Each such path contains at least one cycle and might contain a valid column.
        // With k large enough, we might have actually traversed multiple cycles (or the same multiple times),
        // so we need to make sure we don't return invalid cycles
        
        var distances = new double[duals.Length, duals.Length];
        // due to negative cycles, we must remember all predecessors of previous generations,
        // or we might end up with two or more nodes pointing to each other for the cheapest route to get to a third,
        var predecessor = new int[k][,];
        
        ComputeDistancesFirst();
        ComputeDistancesMid();
        ComputeDistancesLast();

        return GetCycles();
        
        void ComputeDistancesFirst()
        {
            predecessor[0] = new int[duals.Length, duals.Length];

            // initialize distances and predecessors
            // also counts as first iteration
            for (int v = 0; v < duals.Length; v++)
            for (int source = 0; source < duals.Length; source++)
            {
                if (hasArc[source, v])
                {
                    distances[v, source] = arcDistances[source, v];
                    predecessor[0][v, source] = v;
                }
                else
                {
                    distances[v, source] = double.PositiveInfinity;
                    predecessor[0][v, source] = -1;
                }
            }
        }
        
        void ComputeDistancesMid()
        {
            // initialization was implicit first iteration
            for (int iteration = 1; iteration < k - 1; iteration++)
            {
                // we must copy the distances to guarantee we only consider last iteration's distances,
                // or else we might find paths longer than `k`
                var prevDistances = (double[,])distances.Clone();
                var currPreds = predecessor[iteration] = (int[,])predecessor[iteration - 1].Clone();

                for (int u = 0; u < duals.Length; u++)
                for (int v = 0; v < duals.Length; v++)
                {
                    if (!hasArc[u, v]) continue;
                    
                    var arcDistance = arcDistances[u, v];
                
                    // a cycle never goes to a lower node than where it started
                    // such a cycle should (and will) start at the lowest node
                    // therefor, don't consider any source greater than either node in the arc
                    var sourceEnd = Math.Min(u, v) + 1;
                
                    for (int source = 0; source < sourceEnd; source++)
                    {
                        var newDistance = arcDistance + prevDistances[u, source];
                        
                        if (distances[v, source] > newDistance)
                        {
                            distances[v, source] = newDistance;
                            currPreds[v, source] = u;
                        }
                    }
                }
            }
        }
        
        void ComputeDistancesLast()
        {
            // we must copy the distances to guarantee we only consider last iteration's distances,
            // or else we might find paths longer than `k`
            var prevDistances = (double[,])distances.Clone();
            var currPreds = predecessor[k - 1] = (int[,])predecessor[k - 2].Clone();

            for (int u = 0; u < duals.Length; u++)
            for (int v = 0; v < duals.Length; v++)
            {
                if (!hasArc[u, v]) continue;
                    
                var arcDistance = arcDistances[u, v];
                
                int source = v;
                var newDistance = arcDistance + prevDistances[u, source];
                        
                if (distances[v, source] > newDistance)
                {
                    distances[v, source] = newDistance;
                    currPreds[v, source] = u;
                }
            }
        }

        IEnumerable<int[]> GetCycles()
        {
            for (int source = 0; source < duals.Length; source++)
            {
                if (!MasterProblem.IsNegative(distances[source, source]))
                    continue;
            
                var cycle = new List<int>(k);
                var currentNode = source;
                for (int iteration = k - 1; iteration >= 0; iteration--)
                {
                    currentNode = predecessor[iteration][currentNode, source];

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
                if (MasterProblem.IsNegative(actualCost))
                    yield return cycle.ToArray();
            }
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
                cost += -arcWeights[prevNode, node] - duals[node];
                
                prevNode = node;
            }
            
            return cost;
        }
    }
}
