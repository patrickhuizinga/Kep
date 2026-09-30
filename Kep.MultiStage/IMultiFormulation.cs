namespace Kep.MultiStage;

public interface IMultiFormulation
{
    IEnumerable<(double objective, IReadOnlyCollection<int[]> cycles)> SolveMany(
        bool[,] hasArc, double[,] weights, int maxSolutions);
}