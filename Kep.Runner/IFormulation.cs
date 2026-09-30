using Gurobi;

namespace Kep.Runner;

public interface IFormulation
{
    /// <summary>
    /// (Attempts to) solves the specified KEP instance and returns the results.
    /// </summary>
    Result Run(GRBEnv environment, bool[,] A, double[,] w);
}
