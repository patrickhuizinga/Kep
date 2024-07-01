using Gurobi;
using static Kep.Runner.ModelHelper;

namespace Kep.Runner;

/// <summary>
/// Represents the Arc formulation of the KEP that uses row generation to add cycle constraints.
/// </summary>
public class ArcCycleRowGenFormulation(int k) : GurobiFormulation
{
    protected override GRBModel CreateModel(GRBEnv env, bool[,] A, double[,] w)
    {
        var problem = new GRBModel(env);
        var x = problem.AddBinaryVars(A, "x");

        problem.SetObjective(-SumSum(w, x));

        // as many connections in as out
        problem.AddConstrs(x.SumOverI(), GRB.EQUAL, x.SumOverJ(), "in == out");
        // at most 1 connection out
        problem.AddConstrs(x.SumOverJ(), GRB.LESS_EQUAL, 1, "out <= 1");

        problem.Parameters.LazyConstraints = 1;
        problem.SetCallback(new CallBack(x, k));

        return problem;
    }
    
    private sealed class CallBack(GRBVar?[,] x, int k) : GRBCallback
    {
        protected override void Callback()
        {
            if (where != GRB.Callback.MIPSOL)
                return;

            var (lengthI, lengthJ) = x.Dim();
            var arcs = new bool[lengthI, lengthJ];
            for (int i = 0; i < lengthI; i++)
            for (int j = 0; j < lengthJ; j++)
            {
                if (ReferenceEquals(x[i, j], null)) continue;

                arcs[i, j] = GetSolution(x[i, j]) > 0.5;
            }

            foreach (var cycle in GetCycles(arcs))
            {
                if (cycle.Length <= k) continue;

                var expr = new GRBLinExpr();
                var prev = cycle.Last();
                foreach (var next in cycle)
                {
                    expr.AddTerm(1, x[prev, next]);
                    prev = next;
                }
                        
                AddLazy(expr <= cycle.Length - 1);
            }
        }

        private static IEnumerable<int[]> GetCycles(bool[,] arcs)
        {
            return Enumerable.Range(0, arcs.LengthI())
                .SelectMany(i => GetCycles(arcs, [i]));
        }

        private static IEnumerable<int[]> GetCycles(bool[,] arcs, int[] path)
        {
            var first = path[0];
            var current = path.Last();
            
            // return the path if it can make a cycle
            if (arcs[current, first])
                yield return path;
            
            // to prevent duplicates, paths must start with the lowest node
            // therefor don't even evaluate lower nodes 
            for (int j = first + 1; j < arcs.LengthJ(); j++)
            {
                if (!arcs[current, j])
                    continue;
                
                if (path.Contains(j))
                    continue;

                var cycles = GetCycles(arcs, [..path, j]);
                foreach (var cycle in cycles)
                    yield return cycle;
            }
        }
    }
}
