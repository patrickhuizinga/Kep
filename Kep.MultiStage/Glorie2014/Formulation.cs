using System.Diagnostics;
using BranchAndBound;
using Gurobi;
using Kep.Runner;

namespace Kep.MultiStage.Glorie2014;

public class Formulation(int k = 3) : IFormulation
{
    public Result Run(GRBEnv env, bool[,] A, double[,] w)
    {
        var sw = Stopwatch.StartNew();
        
        var solver = new SimpleSolver(
            new BestFirstNodeSelection());
        var problem = new MasterProblem(env, A, w, k);
        var setupTime = sw.Elapsed;
        
        var result = solver.Solve(problem)!;
        var runningTime = sw.Elapsed - setupTime;

        var objective = -result.Objective;
        var gap = 0;

        return new Result(objective, setupTime, runningTime, gap);
    }

}
