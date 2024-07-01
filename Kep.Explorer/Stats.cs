namespace Kep.Explorer;

/// <summary>
/// Represents the min, max, mean and variance of some collection.
/// </summary>
public record Stats(double Min, double Max, double Mean, double Var)
{
    /// <summary>
    /// The standard deviation.
    /// </summary>
    public double Sd => Math.Sqrt(Var);
}