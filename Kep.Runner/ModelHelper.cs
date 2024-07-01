using Gurobi;

namespace Kep.Runner;

/// <summary>
/// Represents a set of static helper methods for creating and solving the KEP.
/// </summary>
public static class ModelHelper
{
    /// <summary>
    /// Generates a KEP instance of size <paramref name="n"/> with the specified <paramref name="density"/> using the
    /// specified <paramref name="seed"/>.
    /// </summary>
    /// <returns>The arcs A and their weights w of the KEP instance.</returns>
    public static (bool[,] A, double[,] w) CreateCompatibility(int n, double density, bool realWeights, int seed)
    {
        var A = new bool[n, n];
        var w = new double[n, n];
        var rng = new Random(seed);
        
        // create the array by start top left and then expand to the bottom right by adding 'rings' / L-shapes
        // that way you ensure a larger size always improves the objective
        for (int i = 0; i < n; i++)
        for (int j = 0; j < i; j++)
        {
            // activation and weight are independent
            // should the density be increased, then the weights will not change
            A[i, j] = rng.NextDouble() < density;
            w[i, j] = rng.NextDouble();

            A[j, i] = rng.NextDouble() < density;
            w[j, i] = rng.NextDouble();
        }

        // we generate the weights even if we don't need them to force the RNG to generate those values
        // and thereby keeping the values for the A matrix the same
        if (!realWeights)
            w = ToDouble(A);

        return (A, w);
    }

    /// <summary>
    /// Converts an array of booleans to an array of doubles. Every <c>true</c> becomes 1 and every <c>false</c> 0.
    /// </summary>
    public static double[,] ToDouble(bool[,] b)
    {
        var (lengthI, lengthJ) = b.Dim();
        var result = new double[lengthI, lengthJ];
        
        for (int i = 0; i < lengthI; i++)
        for (int j = 0; j < lengthJ; j++)
            result[i, j] = b[i, j] ? 1 : 0;

        return result;
    }

    /// <summary>
    /// Returns a single <see cref="GRBLinExpr"/> that sums over all the specified <paramref name="variables"/>, each
    /// multiplied by its corresponding <paramref name="weights"/>.
    /// </summary>
    /// <remarks>Any <c>null</c> item in <paramref name="variables"/> will be skipped.</remarks>
    public static GRBLinExpr SumSum(double[,] weights, GRBVar?[,] variables)
    {
        var (lengthI, lengthJ) = variables.Dim();

        var sum = new GRBLinExpr();
        for (int i = 0; i < lengthI; i++)
        for (int j = 0; j < lengthJ; j++)
        {
            if (!ReferenceEquals(variables[i, j], null))
                sum.AddTerm(weights[i, j], variables[i, j]);
        }

        return sum;
    }

    /// <summary>
    /// Returns a single <see cref="GRBLinExpr"/> that sums over all the specified <paramref name="expressions"/>, each
    /// multiplied by its corresponding <paramref name="weights"/>.
    /// </summary>
    /// <remarks>Any <c>null</c> item in <paramref name="expressions"/> will be skipped.</remarks>
    public static GRBLinExpr SumSum(double[,] weights, GRBLinExpr?[,] expressions)
    {
        var (lengthI, lengthJ) = expressions.Dim();

        var sum = new GRBLinExpr();
        for (int i = 0; i < lengthI; i++)
        for (int j = 0; j < lengthJ; j++)
        {
            if (!ReferenceEquals(expressions[i, j], null))
                sum.MultAdd(weights[i, j], expressions[i, j]);
        }

        return sum;
    }
}
