namespace Kep.Explorer;

/// <summary>
/// Represents a program that can filter, sort and show the results from Kep.Runner.
/// </summary>
public static class Program
{
    private static readonly List<string> Formulations = [];
    private static readonly List<string> N = [];
    private static readonly List<string> K = [];
    private static readonly List<string> Densities = [];
    private static readonly List<string> W = [];
    private static readonly List<string> Setup = [];
    private static readonly List<string> Run = [];
    private static readonly List<string> Total = [];
    private static readonly List<string> Gap = [];

    private static readonly List<string> SortProps = [];
    private static readonly List<string> Show = [];
    
    public static void Main(string[] args)
    {
        if (args is [])
            args = ["output.dat"];
        
        var lines = args.SelectMany(File.ReadAllLines).ToList();
        var results = lines.Select(Result.Parse).ToList();
        
        Show.Add("count");
        Display(results);
        
        while (true)
        {
            Console.Write("> ");
            var command = Console.ReadLine();
            if (String.IsNullOrWhiteSpace(command))
                continue;
            if ("quit".StartsWith(command))
                return;

            ParseCommand(command);
            var filtered = Filter(results);
            var sorted = Sort(filtered);
            Display(sorted);
        }
    }

    private static void ParseCommand(string command)
    {
        Show.Clear();
        Show.Add("count");
        
        var current = Show;
        var parts = command.Split();
        foreach (var arg in parts)
        {
            switch (arg.ToLower())
            {
                case "f":
                    current = Formulations;
                    break;
                case "n":
                    current = N;
                    break;
                case "k":
                    current = K;
                    break;
                case "d":
                    current = Densities;
                    break;
                case "w":
                    current = W;
                    break;
                case "setup":
                    current = Setup;
                    break;
                case "run":
                    current = Run;
                    break;
                case "t":
                case "total":
                    current = Total;
                    break;
                case "g":
                case "gap":
                    current = Gap;
                    break;
                
                case "sort":
                    current = SortProps;
                    break;
                case "show":
                    current = Show;
                    break;
                case "reset":
                    Formulations.Clear();
                    N.Clear();
                    K.Clear();
                    Densities.Clear();
                    W.Clear();
                    Setup.Clear();
                    Run.Clear();
                    Total.Clear();
                    Gap.Clear();
                    SortProps.Clear();
                    break;
                default:
                    if (arg.StartsWith('+'))
                    {
                        current.AddRange(arg[1..].Split(','));
                    }
                    else if (arg.StartsWith('-'))
                    {
                        foreach (var part in arg[1..].Split(',')) 
                            current.Remove(part);
                    }
                    else if (arg == "*")
                    {
                        current.Clear();
                    }
                    else
                    {
                        current.Clear();
                        current.AddRange(arg.Split(','));
                    }
                    break;
            }
        }
    }

    private static List<Result> Filter(List<Result> allResults)
    {
        var result = new List<Result>(allResults);
        if (Formulations.Count > 0) 
            result.RemoveAll(r => !Formulations.Contains(r.Formulation));
        if (N.Count > 0)
            FilterNumbers(result, N, r => r.N);
        if (Densities.Count > 0)
            FilterNumbers(result, Densities, r => r.D);
        if (K.Count > 0)
            FilterNumbers(result, K, r => r.K);
        if (W.Count > 0)
            FilterNumbers(result, W, r => r.W);
        if (Setup.Count > 0)
            FilterNumbers(result, Setup, r => r.Setup);
        if (Run.Count > 0)
            FilterNumbers(result, Run, r => r.Run);
        if (Total.Count > 0)
            FilterNumbers(result, Total, r => r.TotalTime);
        if (Gap.Count > 0)
            FilterNumbers(result, Gap, r => r.Gap);

        return result;
    }

    private static void FilterNumbers(List<Result> result, List<string> filters, Func<Result, int> propSelector)
    {
        if (filters.All(f => !f.Contains("..")))
        {
            result.RemoveAll(r => !filters.Contains(propSelector(r).ToString()));
            return;
        }

        int min = 0, max = 0;
        foreach (var filter in filters)
        {
            if (filter.StartsWith(".."))
            {
                min = 0;
                max = int.Parse(filter[2..]);
            } else if (filter.EndsWith(".."))
            {
                min = int.Parse(filter[..^2]);
                max = int.MaxValue;
            }
            else
            {
                var parts = filter.Split("..");
                min = int.Parse(parts[0]);
                max = int.Parse(parts[1]);
            }
        }

        result.RemoveAll(r =>
        {
            var prop = propSelector(r);
            return prop < min || max < prop;
        });
    }

    private static void FilterNumbers(List<Result> result, List<string> filters, Func<Result, double> propSelector)
    {
        if (filters.All(f => !f.Contains("..")))
        {
            var values = filters.Select(double.Parse).ToList();
            result.RemoveAll(r => !values.Contains(propSelector(r)));
            return;
        }

        double min = 0, max = 0;
        foreach (var filter in filters)
        {
            if (filter.StartsWith(".."))
            {
                min = 0;
                max = double.Parse(filter[2..]);
            } else if (filter.EndsWith(".."))
            {
                min = double.Parse(filter[..^2]);
                max = double.MaxValue;
            }
            else
            {
                var parts = filter.Split("..");
                min = double.Parse(parts[0]);
                max = double.Parse(parts[1]);
            }
        }

        result.RemoveAll(r =>
        {
            var prop = propSelector(r);
            return prop < min || max < prop;
        });
    }

    private static List<Result> Sort(List<Result> results)
    {
        if (SortProps is [])
            return results;

        var comparison = SortProps.Select(GetComparison)
            .Aggregate(Combine);
        
        var sorted = new List<Result>(results);
        sorted.Sort(comparison);
        return sorted;

        Comparison<T> Combine<T>(Comparison<T> comp1, Comparison<T> comp2)
        {
            return (x, y) =>
            {
                var first = comp1(x, y);
                return first == 0 ? comp2(x, y) : first;
            };
        }
    }

    private static Comparison<Result> GetComparison(string prop)
    {
        return prop switch
        {
            "f" or "formulation" => (r1, r2) => String.CompareOrdinal(r1.Formulation, r2.Formulation),
            "n" => (r1, r2) => r1.N.CompareTo(r2.N),
            "k" => (r1, r2) => r1.K.CompareTo(r2.K),
            "d" => (r1, r2) => r1.D.CompareTo(r2.D),
            "w" => (r1, r2) => r1.W.CompareTo(r2.W),
            "s" or "seed" => (r1, r2) => r1.Seed.CompareTo(r2.Seed),
            "setup" => (r1, r2) => r1.Setup.CompareTo(r2.Setup),
            "run" => (r1, r2) => r1.Run.CompareTo(r2.Run),
            "t" or "total" => (r1, r2) => r1.TotalTime.CompareTo(r2.TotalTime),
            "g" or "gap" => (r1, r2) => r1.Gap.CompareTo(r2.Gap),
            // objective values are sorted in descending order
            "o" or "obj" or "objective" => (r1, r2) => -r1.ObjVal.CompareTo(r2.ObjVal),
            _ => throw new ArgumentOutOfRangeException(nameof(prop), prop, "unknown property: " + prop)
        };
    }

    private static void Display(List<Result> results)
    {
        foreach (var showCommand in Show)
        {
            switch (showCommand.ToLower())
            {
                case "count":
                    Console.WriteLine(results.Count + " results");
                    break;
                case "all":
                    Console.WriteLine(Result.Header);
                    foreach (var result in results)
                        Console.WriteLine(result);
                    break;
                case "mean":
                    var resultMeans = results.GroupByAdjacent(
                        r => (r.Formulation, r.N, r.D, r.K, r.W),
                        (_, group) => Result.Mean(group));

                    Console.WriteLine(ResultMean.Header);
                    foreach (var resultMean in resultMeans)
                        Console.WriteLine(resultMean);
                    break;
                case "summary":
                    var formulations = results.Select(r => r.Formulation).Distinct().Order(StringComparer.OrdinalIgnoreCase);
                    Console.WriteLine("formulations: " + string.Join(", ", formulations));
                    Console.Write("n: " + string.Join(", ", results.Select(r => r.N).Distinct()));
                    Console.Write("    d: " + string.Join(", ", results.Select(r => r.D).Distinct()));
                    Console.Write("    w: " + string.Join(", ", results.Select(r => r.W).Distinct()));
                    Console.WriteLine("    k: " + string.Join(", ", results.Select(r => r.K).Distinct()));
                    Console.WriteLine("time: " + results.Stats(r => r.TotalTime));
                    Console.WriteLine("gap: " + results.Stats(r => r.Gap));
                    break;
            }
        }
    }
}
