using Gurobi;

using static Kep.Runner.ModelHelper;

namespace Kep.Runner;

/// <summary>
/// Represents the Arc formulation of the KEP.
/// </summary>
public class ArcPathFormulation(int k) : GurobiFormulation
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
        
        foreach (var path in GetPaths(A))
        {
            var expr = new GRBLinExpr();
            var prev = path[0];
            for (int i = 1; i < path.Length; i++)
            {
                var next = path[i];
                expr.AddTerm(1, x[prev, next]);
                prev = next;
            }

            problem.AddConstr(expr <= k - 1, "long path");
        }
        
        return problem;
    }

    private IEnumerable<int[]> GetPaths(bool[,] arcs)
    {
        return Enumerable.Range(0, arcs.LengthI())
            .SelectMany(i => GetPaths(arcs, [i]));
    }

    private IEnumerable<int[]> GetPaths(bool[,] arcs, int[] path)
    {
        if (path.Length == k + 1)
        {
            yield return path;
            yield break;
        }
        
        var current = path.Last();
        for (int j = 0; j < arcs.LengthJ(); j++)
        {
            if (!arcs[current, j])
                continue;
            
            if (path.Contains(j))
                continue;

            var paths = GetPaths(arcs, [..path, j]);
            foreach (var p in paths)
                yield return p;
        }
    }
}
