using Gurobi;

using static Kep.Runner.ModelHelper;

namespace Kep.Runner;

/// <summary>
/// Represents the Extended Edge formulation of the KEP.
/// </summary>
public class ExtendedEdgeFormulation(int k) : GurobiFormulation
{
    protected override GRBModel CreateModel(GRBEnv env, bool[,] A, double[,] w)
    {
        var n = A.LengthI();

        var xTemplate = CreateLayers(A, n, k);

        var problem = new GRBModel(env);
        
        var x = problem.AddBinaryVars(xTemplate);

        var xSumOverJ = x.SumOverJ();

        // 9a
        problem.SetObjective(-SumSum(w, x.SumOverK()));

        // 9b
        problem.AddConstrs(x.SumOverI(), GRB.EQUAL, xSumOverJ, "9b");
        // 9c
        problem.AddConstrs(x.SumSumOverJK(), GRB.LESS_EQUAL, 1, "9c");
        // 9d
        problem.AddConstrs(x.SumSumOverIJ(), GRB.LESS_EQUAL, k, "9d");
        // 9e
        for (int l = 0; l < n; l++)
        for (int i = l + 1; i < n; i++)
            problem.AddConstr(xSumOverJ[i, l] <= xSumOverJ[l, l], $"9e^{l}_{i}");
        
        return problem;
    }

    private static bool[,,] CreateLayers(bool[,] A, int n, int k)
    {
        var d = GetShortestPathDistances(A);
        
        var result = new bool[n, n, n];
        for (int l = 0; l < n; l++)
        {
            for (int i = l; i < n; i++)
            for (int j = l; j < n; j++)
            {
                if (i == j) continue;
                if (!A[i, j]) continue;

                if (d[l, i] + 1 + d[j, l] <= k)
                    result[i, j, l] = true;
            }
        }

        return result;
    }

    private static int[,] GetShortestPathDistances(bool[,] A)
    {
        var (lengthI, lengthJ) = A.Dim();
        var result = new int[lengthI, lengthJ];
        
        for (int i = 0; i < lengthI; i++)
        for (int j = 0; j < lengthJ; j++)
        {
            if (i == j)
                result[i, j] = 0;
            else if (A[i, j])
                result[i, j] = 1;
            else
                // some large number that we can still safely sum
                result[i, j] = int.MaxValue / 2;
        }

        bool hasChanges = true;
        while (hasChanges)
        {
            hasChanges = false;
            for (int i = 0; i < lengthI; i++)
            for (int j = 0; j < lengthJ; j++)
            for (int k = 0; k < lengthI; k++)
            {
                if (result[i, j] + result[j, k] < result[i, k])
                {
                    result[i, k] = result[i, j] + result[j, k];
                    hasChanges = true;
                }
            }
        }

        return result;
    }
}
