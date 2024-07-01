using Gurobi;

namespace Kep.Runner;

/// <summary>
/// Represents a set of extension methods for <see cref="GRBModel"/>.
/// </summary>
public static class ModelExtensions
{
    /// <summary>
    /// Adds <paramref name="length"/> number of integer variables to <paramref name="model"/>. All variables have the
    /// specified <paramref name="bounds"/>.
    /// </summary>
    /// <returns>An array of the specified <paramref name="length"/> with the added variables.</returns>
    public static GRBVar[] AddVars(this GRBModel model, (int lower, int upper) bounds, int length, string name = "x")
    {
        var result = new GRBVar[length];

        for (int i = 0; i < length; i++) 
            result[i] = model.AddVar(bounds.lower, bounds.upper, 0, GRB.INTEGER, $"{name}[{i}]");
        
        return result;
    }
    
    /// <summary>
    /// Adds an integer variable per item in <paramref name="bounds"/> to <paramref name="model"/>, each with the bounds
    /// as specified by that item..
    /// </summary>
    /// <returns>An array with the added variables.</returns>
    public static GRBVar[] AddVars(this GRBModel model, IEnumerable<(int lower, int upper)> bounds, string name = "x")
    {
        if (!bounds.TryGetNonEnumeratedCount(out var count))
        {
            bounds = bounds.ToList();
            count = bounds.Count();
        }
        
        var result = new GRBVar[count];

        int i = 0;
        foreach (var bound in bounds) 
            result[i++] = model.AddVar(bound.lower, bound.upper, 0, GRB.INTEGER, $"{name}[{i}]");

        return result;
    }

    /// <summary>
    /// Adds a binary variable to <paramref name="model"/> and returns it.
    /// </summary>
    public static GRBVar AddBinaryVar(this GRBModel model, string name = "x")
    {
        return model.AddVar(0, 1, 0, GRB.BINARY, name);
    }

    /// <summary>
    /// Adds <paramref name="length"/> number of binary variables to <paramref name="model"/>.
    /// </summary>
    /// <returns>An array of the specified <paramref name="length"/> with the added variables.</returns>
    public static GRBVar[] AddBinaryVars(this GRBModel model, int length, string name = "x")
    {
        var result = new GRBVar[length];

        for (int i = 0; i < length; i++) 
            result[i] = model.AddBinaryVar($"{name}[{i}]");

        return result;
    }

    /// <summary>
    /// For each item in <paramref name="template"/>, adds a binary variable to <paramref name="model"/> if that item is
    /// <c>true</c>.
    /// </summary>
    /// <returns>
    /// An array with the same dimensions as <paramref name="template"/> that contains the added variables and otherwise
    /// contains <c>null</c>.
    /// </returns>
    public static GRBVar?[] AddBinaryVars(this GRBModel model, bool[] template, string name = "x")
    {
        var length = template.Dim();
        var result = new GRBVar?[length];

        for (int i = 0; i < length; i++)
        {
            if (template[i])
                result[i] = model.AddBinaryVar($"{name}[{i}]");
            else
                result[i] = null;
        }

        return result;
    }

    /// <summary>
    /// For each item in <paramref name="template"/>, adds a binary variable to <paramref name="model"/> if that item is
    /// <c>true</c>.
    /// </summary>
    /// <returns>
    /// An array with the same dimensions as <paramref name="template"/> that contains the added variables and otherwise
    /// contains <c>null</c>.
    /// </returns>
    public static GRBVar?[,] AddBinaryVars(this GRBModel model, bool[,] template, string name = "x")
    {
        var (lengthI, lengthJ) = template.Dim();
        var result = new GRBVar?[lengthI, lengthJ];

        for (int i = 0; i < lengthI; i++)
        for (int j = 0; j < lengthJ; j++)
        {
            if (template[i, j])
                result[i, j] = model.AddBinaryVar($"{name}[{i},{j}]");
            else
                result[i, j] = null;
        }
        
        return result;
    }

    /// <summary>
    /// For each item in <paramref name="template"/>, adds a binary variable to <paramref name="model"/> if that item is
    /// <c>true</c>.
    /// </summary>
    /// <returns>
    /// An array with the same dimensions as <paramref name="template"/> that contains the added variables and otherwise
    /// contains <c>null</c>.
    /// </returns>
    public static GRBVar?[,,] AddBinaryVars(this GRBModel model, bool[,,] template, string name = "x")
    {
        var (lengthI, lengthJ, lengthK) = template.Dim();
        var result = new GRBVar?[lengthI, lengthJ, lengthK];

        for (int i = 0; i < lengthI; i++)
        for (int j = 0; j < lengthJ; j++)
        for (int k = 0; k < lengthK; k++)
        {
            if (template[i, j, k])
                result[i, j, k] = model.AddBinaryVar($"{name}[{i},{j},{k}]");
            else
                result[i, j, k] = null;
        }

        return result;
    }

    /// <summary>
    /// Adds a constraint to <paramref name="model"/> for each element of <paramref name="left"/> that represents a
    /// comparison with <paramref name="right"/> of type <paramref name="sense"/>.
    /// </summary>
    public static void AddConstrs(this GRBModel model, GRBLinExpr[] left, char sense, GRBLinExpr right, string name = "constr")
    {
        for (int i = 0; i < left.Length; i++)
            model.AddConstr(left[i], sense, right, $"{name}[{i}]");
    }

    /// <summary>
    /// Adds a constraint to <paramref name="model"/> for each element of <paramref name="left"/> that represents a
    /// comparison of type <paramref name="sense"/> with the corresponding element in <paramref name="right"/>.
    /// </summary>
    public static void AddConstrs(this GRBModel model, GRBLinExpr[] left, char sense, GRBLinExpr[] right, string name = "constr")
    {
        for (int i = 0; i < left.Length; i++)
            model.AddConstr(left[i], sense, right[i], $"{name}[{i}]");
    }

    /// <summary>
    /// Adds a constraint to <paramref name="model"/> for each element of <paramref name="left"/> that represents a
    /// comparison of type <paramref name="sense"/> with the corresponding element in <paramref name="right"/>.
    /// </summary>
    public static void AddConstrs(this GRBModel model, GRBLinExpr[,] left, char sense, GRBLinExpr[,] right, string name = "constr")
    {
        var (lengthI, lengthJ) = left.Dim();
        
        for (int i = 0; i < lengthI; i++)
        for (int j = 0; j < lengthJ; j++)
            model.AddConstr(left[i, j], sense, right[i, j], $"{name}[{i},{j}]");
    }
}