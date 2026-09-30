namespace Kep.MultiStage.Glorie2014;

public interface IPricingProblem
{
    IEnumerable<int[]> SolveMany(double[] duals);
}
