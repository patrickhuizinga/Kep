using System.Diagnostics.CodeAnalysis;
using Gurobi;

namespace Kep.Runner;

/// <summary>
/// Represents a set of extension methods for <see cref="GRBVar"/>.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class VarExtensions
{
    /// <summary>
    /// Returns expressions that represent \sum_i(var_ij) \forall j
    /// </summary>
    /// <returns>An array with the same size as the second dimension of <paramref name="variables"/>.</returns>
    public static GRBLinExpr[] SumOverI(this GRBVar?[,] variables)
    {
        var (lengthI, lengthJ) = variables.Dim();

        var result = new GRBLinExpr[lengthJ];
        for (int j = 0; j < lengthJ; j++)
        {
            var sum = new GRBLinExpr();
            for (int i = 0; i < lengthI; i++)
            {
                if (!ReferenceEquals(variables[i, j], null))
                    sum.AddTerm(1, variables[i, j]);
            }

            result[j] = sum;
        }

        return result;
    }

    /// <summary>
    /// Returns expressions that represent \sum_j(var_ij) \forall i
    /// </summary>
    /// <returns>An array with the same size as the first dimension of <paramref name="variables"/>.</returns>
    public static GRBLinExpr[] SumOverJ(this GRBVar?[,] variables)
    {
        var (lengthI, lengthJ) = variables.Dim();

        var result = new GRBLinExpr[lengthI];
        for (int i = 0; i < lengthI; i++)
        {
            var sum = new GRBLinExpr();
            for (int j = 0; j < lengthJ; j++)
            {
                if (!ReferenceEquals(variables[i, j], null))
                    sum.AddTerm(1, variables[i, j]);
            }

            result[i] = sum;
        }

        return result;
    }

    /// <summary>
    /// Returns expressions that represent \sum_i(var_ijk) \forall j,k
    /// </summary>
    /// <returns>An array with the same size as the second and third dimensions of <paramref name="variables"/>.</returns>
    public static GRBLinExpr[,] SumOverI(this GRBVar?[,,] variables)
    {
        var (countI, countJ, countK) = variables.Dim();

        var result = new GRBLinExpr[countJ, countK];
        for (int j = 0; j < countJ; j++)
        for (int k = 0; k < countK; k++)
        {
            var sum = new GRBLinExpr();
            for (int i = 0; i < countI; i++)
            {
                if (!ReferenceEquals(variables[i, j, k], null))
                    sum.AddTerm(1, variables[i, j, k]);
            }

            result[j, k] = sum;
        }

        return result;
    }

    /// <summary>
    /// Returns expressions that represent \sum_j(var_ijk) \forall i,k
    /// </summary>
    /// <returns>An array with the same size as the first and third dimensions of <paramref name="variables"/>.</returns>
    public static GRBLinExpr[,] SumOverJ(this GRBVar?[,,] variables)
    {
        var (countI, countJ, countK) = variables.Dim();

        var result = new GRBLinExpr[countI, countK];
        for (int i = 0; i < countI; i++)
        for (int k = 0; k < countK; k++)
        {
            var sum = new GRBLinExpr();
            for (int j = 0; j < countJ; j++)
            {
                if (!ReferenceEquals(variables[i, j, k], null))
                    sum.AddTerm(1, variables[i, j, k]);
            }

            result[i, k] = sum;
        }

        return result;
    }

    /// <summary>
    /// Returns expressions that represent \sum_k(var_ijk) \forall i,j
    /// </summary>
    /// <returns>An array with the same size as the first and second dimensions of <paramref name="variables"/>.</returns>
    public static GRBLinExpr[,] SumOverK(this GRBVar?[,,] variables)
    {
        var (countI, countJ, countK) = variables.Dim();

        var result = new GRBLinExpr[countI, countJ];
        for (int i = 0; i < countI; i++)
        for (int j = 0; j < countJ; j++)
        {
            var sum = new GRBLinExpr();
            for (int k = 0; k < countK; k++)
            {
                if (!ReferenceEquals(variables[i, j, k], null))
                    sum.AddTerm(1, variables[i, j, k]);
            }

            result[i, j] = sum;
        }

        return result;
    }

    /// <summary>
    /// Returns expressions that represent \sum_i\sum_j(var_ijk) \forall k
    /// </summary>
    /// <returns>An array with the same size as the third dimensions of <paramref name="variables"/>.</returns>
    public static GRBLinExpr[] SumSumOverIJ(this GRBVar?[,,] variables)
    {
        var (countI, countJ, countK) = variables.Dim();

        var result = new GRBLinExpr[countK];
        for (int k = 0; k < countK; k++)
        {
            var sum = new GRBLinExpr();
            for (int i = 0; i < countI; i++)
            for (int j = 0; j < countJ; j++)
            {
                if (!ReferenceEquals(variables[i, j, k], null))
                    sum.AddTerm(1, variables[i, j, k]);
            }

            result[k] = sum;
        }

        return result;
    }

    /// <summary>
    /// Returns expressions that represent \sum_i\sum_k(var_ijk) \forall j
    /// </summary>
    /// <returns>An array with the same size as the second dimensions of <paramref name="variables"/>.</returns>
    public static GRBLinExpr[] SumSumOverIK(this GRBVar?[,,] variables)
    {
        var (countI, countJ, countK) = variables.Dim();

        var result = new GRBLinExpr[countJ];
        for (int j = 0; j < countJ; j++)
        {
            var sum = new GRBLinExpr();
            for (int i = 0; i < countI; i++)
            for (int k = 0; k < countK; k++)
            {
                if (!ReferenceEquals(variables[i, j, k], null))
                    sum.AddTerm(1, variables[i, j, k]);
            }

            result[j] = sum;
        }

        return result;
    }

    /// <summary>
    /// Returns expressions that represent \sum_j\sum_k(var_ijk) \forall i
    /// </summary>
    /// <returns>An array with the same size as the first dimensions of <paramref name="variables"/>.</returns>
    public static GRBLinExpr[] SumSumOverJK(this GRBVar?[,,] variables)
    {
        var (countI, countJ, countK) = variables.Dim();

        var result = new GRBLinExpr[countI];
        for (int i = 0; i < countI; i++)
        {
            var sum = new GRBLinExpr();
            for (int j = 0; j < countJ; j++)
            for (int k = 0; k < countK; k++)
            {
                if (!ReferenceEquals(variables[i, j, k], null))
                    sum.AddTerm(1, variables[i, j, k]);
            }

            result[i] = sum;
        }

        return result;
    }
}
