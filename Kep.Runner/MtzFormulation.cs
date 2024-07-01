using Gurobi;

using static Kep.Runner.ModelHelper;

namespace Kep.Runner;

/// <summary>
/// Represents the MTZ-like formulation of the KEP.
/// </summary>
public class MtzFormulation(int k) : GurobiFormulation
{
    protected override GRBModel CreateModel(GRBEnv env, bool[,] A, double[,] w)
    {
        var n = A.LengthI();

        var problem = new GRBModel(env);
        var x = problem.AddBinaryVars(A, "x");

        // cycle_i has range i..n
        var cycle = problem.AddVars(Enumerable.Range(0, n).Select(i => (lower: i, upper: n-1)), "y");
        var positionInCycle = problem.AddVars((lower: 1, upper: k), n, "u");
        var isLastInCycle = problem.AddBinaryVars(n, "z");
        
        problem.SetObjective(-SumSum(w, x));

        // as many connections in as out
        problem.AddConstrs(x.SumOverI(), GRB.EQUAL, x.SumOverJ(), "in == out");
        // at most 1 connection out
        problem.AddConstrs(x.SumOverJ(), GRB.LESS_EQUAL, 1, "out <= 1");

        // isLast_i == 1  =>  cycle_i == i
        for (int i = 0; i < n; i++)
        {
            problem.AddConstr(cycle[i] - i <= n * (1 - isLastInCycle[i]), $"z[{i}]==1 => y[{i}]=={i}");
            // cycle_i >= i is a part of the definition
        }
        
        // x_ij == 1  =>  cycle_i == cycle_j
        foreach (var (i, j) in A.Indices())
        {
            problem.AddConstr(cycle[i] - cycle[j] <= n * (1 - x[i, j]), $"x[{i},{j}]==1 => y[{i}]==y[{j}]");
            problem.AddConstr(cycle[j] - cycle[i] <= n * (1 - x[i, j]), $"x[{i},{j}]==1 => y[{i}]==y[{j}]");
        }
        
        // x_ij == 1  =>  position_i + 1 <= position_j  or  isLast_i == 1 
        foreach (var (i, j) in A.Indices())
        {
            problem.AddConstr(positionInCycle[i] + 1 - positionInCycle[j] <= k * (1 - x[i, j] + isLastInCycle[i]), $"x[{i},{j}]==1 => u[{i}]<u[{j}] or z[{i}==1");
        }
        
        return problem;
    }
}
