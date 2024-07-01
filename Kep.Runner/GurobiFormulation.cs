using System.Diagnostics;
using Gurobi;

namespace Kep.Runner;

/// <summary>
/// Represents the implementation of a KEP formulation.
/// </summary>
public abstract class GurobiFormulation
{
    /// <summary>
    /// (Attempts to) solves the specified KEP instance and returns the results.
    /// </summary>
    public Result Run(GRBEnv environment, bool[,] A, double[,] w)
    {
        var sw = Stopwatch.StartNew();
        
        var problem = CreateModel(environment, A, w);
        var setupTime = sw.Elapsed;
        
        problem.Optimize();
        var runningTime = TimeSpan.FromSeconds(problem.Runtime);

        var objective = -problem.ObjVal;
        var gap = problem.MIPGap;
        
        return new Result(objective, setupTime, runningTime, gap);
    }

    /// <summary>
    /// Creates a <see cref="GRBModel"/> that represents the specified KEP instance.
    /// </summary>
    protected abstract GRBModel CreateModel(GRBEnv env, bool[,] A, double[,] w);
}
