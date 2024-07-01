using System.Globalization;

namespace Kep.Explorer;

/// <summary>
/// Represents the configuration and result of a single instance of solving the KEP.
/// </summary>
public sealed record Result(
    string Formulation,
    int N,
    int K,
    int D,
    int Seed,
    int W,
    int Setup,
    int Run,
    double ObjVal,
    double Gap)
{
    public const string Header = "formulation        n  k d% sd w? setu run tot.  objective        gap        UB";

    /// <summary>
    /// Calculates the upper bound based on <see cref="ObjVal"/> and <see cref="Gap"/>.
    /// </summary>
    public double UB => ObjVal + ObjVal * Gap;

    /// <summary>
    /// Calculates the total time spent by this instance, based on <see cref="Setup"/> and <see cref="Run"/>.
    /// </summary>
    public int TotalTime => Math.Min(1800, Setup + Run);

    public static Result Parse(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
        var formulation = parts[0];
        var n = int.Parse(parts[1]);
        var k = int.Parse(parts[2]);
        var d = int.Parse(parts[3]);
        var a = int.Parse(parts[4]);
        var l = int.Parse(parts[5]);
        var seed = int.Parse(parts[6]);
        var w = int.Parse(parts[7]);
        var setup = int.Parse(parts[8]);
        var run = int.Parse(parts[9]);
        run = Math.Min(1800, run);
        var objective = double.Parse(parts[10]);
        var gap = double.Parse(parts[11]);
            
        return new Result(formulation, n, k, d, seed, w, setup, run, objective, gap);
    }

    public static ResultMean Mean(IReadOnlyCollection<Result> results)
    {
        results = results.ToList();
        var first = results.First();
        return new ResultMean(
            first.Formulation,
            first.N,
            first.K,
            first.D,
            first.W,
            results.Count(),
            results.Stats(r => r.Setup),
            results.Stats(r => r.Run),
            results.Stats(r => r.TotalTime),
            results.Stats(r => r.Gap));
    }

    public override string ToString()
    {
        return $"{Formulation,-15} {N,4} {K,2} {D,2} {Seed,3} {W,1} {Setup,3} {Run,4} {TotalTime,4} {Left(ObjVal, 10)} {Left(Gap, 10)}, {Left(UB, 10)}";
    }

    private static string Left(double number, int length)
    {
        string output = "";
        int decimalPlaces = length - 2; //because every decimal contains at least "0."
        bool isError = true;

        while (isError && decimalPlaces >= 0)
        {
            output = Math.Round(number, decimalPlaces).ToString(NumberFormatInfo.InvariantInfo);
            isError = output.Length > length;
            decimalPlaces--;
        }

        if (isError)
            throw new FormatException($"{number} can't be represented in {length} characters");
        
        return output.PadLeft(length);
    }
}