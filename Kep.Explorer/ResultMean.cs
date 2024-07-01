using System.Globalization;

namespace Kep.Explorer;

/// <summary>
/// Represents a summary of the results of multiple instances.
/// </summary>
public sealed record ResultMean(
    string Formulation,
    int N,
    int K,
    int D,
    int W,
    int Count,
    Stats Setup,
    Stats Run,
    Stats Total,
    Stats Gap)
{
    public const string Header = "formulation        n  k d% w? #     setup          run        total           gap    ";

    public override string ToString()
    {
        return $"{Formulation,-15} {N,4} {K,2} {D,2} {W,1} {Count,2}  {Left(Setup.Mean,5)} ±{Right(Setup.Sd,5)}  {Left(Run.Mean,5)} ±{Right(Run.Sd,5)}  {Left(Total.Mean,5)} ±{Right(Total.Sd,5)}  {Left(Gap.Mean,5)} ±{Right(Gap.Sd,5)}";
    }

    private static string Left(double number, int totalWidth)
    {
        var max = Math.Pow(10, totalWidth) - 1;
        if (number > max) 
            number = max;
            
        string output = "";
        int decimalPlaces = totalWidth - 2; //because every decimal contains at least "0."
        bool isError = true;

        while (isError && decimalPlaces >= 0)
        {
            output = Math.Round(number, decimalPlaces).ToString(NumberFormatInfo.InvariantInfo);
            isError = output.Length > totalWidth;
            decimalPlaces--;
        }

        if (isError)
        {
            throw new FormatException($"{number} can't be represented in {totalWidth} characters");
        }
        
        return output.PadLeft(totalWidth);
    }

    private static string Right(double number, int totalWidth)
    {
        var max = Math.Pow(10, totalWidth) - 1;
        if (number > max) 
            number = max;

        string output = "";
        int decimalPlaces = totalWidth - 2; //because every decimal contains at least "0."
        bool isError = true;

        while (isError && decimalPlaces >= 0)
        {
            output = Math.Round(number, decimalPlaces).ToString(NumberFormatInfo.InvariantInfo);
            isError = output.Length > totalWidth;
            decimalPlaces--;
        }

        if (isError)
        {
            throw new FormatException($"{number} can't be represented in {totalWidth} characters");
        }
        
        return output.PadRight(totalWidth);
    }
}
