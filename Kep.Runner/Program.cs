using System.Diagnostics;
using System.Globalization;
using Gurobi;

namespace Kep.Runner;

/// <summary>
/// Represents a program that can run one or more KEP formulations.
/// </summary>
public static class Program
{
    private static readonly List<string> Formulations = [];
    private static readonly List<int> N = [];
    private static readonly List<int> K = [];
    private static readonly List<double> Densities = [];
    private static int _threads = 1;
    private static readonly SortedSet<bool> RealWeights = [];
    // todo: one day support altruists
    private static readonly List<double> Altruists = [];
    private static readonly List<int> L = [];

    private static readonly TimeSpan GurobiTimeLimit = TimeSpan.FromMinutes(30);

    public static void Main(string[] args)
    {
        ParseArguments(args);

        if (Formulations.Count == 0)
            throw new ArgumentException("Should provide at least 1 formulation to run");
        
        ApplyDefaults();

        if (HasMultipleConfigurations)
            StartChildProcesses();
        else
            RunModel();
    }

    /// <summary>
    /// Returns whether multiple configurations are specified, in other words, whether at least one configuration
    /// element has multiple values. 
    /// </summary>
    private static bool HasMultipleConfigurations
        => Formulations.Count > 1 || N.Count > 1 || K.Count > 1 || Densities.Count > 1 || RealWeights.Count > 1;

    /// <summary>
    /// Parses the configuration values.
    /// </summary>
    private static void ParseArguments(string[] args)
    {
        Action<string> add = arg => Formulations.Add(arg);
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "n":
                    add = arg => N.Add(int.Parse(arg));
                    break;
                case "k":
                    add = arg => K.Add(int.Parse(arg));
                    break;
                case "d":
                    add = arg => Densities.Add(double.Parse(arg, CultureInfo.InvariantCulture));
                    break;
                case "t":
                    add = arg => _threads = int.Parse(arg);
                    break;
                case "w":
                    add = arg => RealWeights.Add(bool.Parse(arg));

                    // if the next argument (if any) is not a bool, then default is `true`
                    if (args.Length == i + 1 || !bool.TryParse(args[i + 1], out _))
                        RealWeights.Add(true);
                    break;
                default:
                    add(args[i]);
                    break;
            }
        }
    }

    /// <summary>
    /// For any configuration that is not specified, adds its default value.
    /// </summary>
    private static void ApplyDefaults()
    {
        if (N.Count == 0) N.Add(10);
        if (K.Count == 0) K.Add(3);
        if (Densities.Count == 0) Densities.Add(0.2);
        if (RealWeights.Count == 0) RealWeights.Add(false);
    }

    private static void RunModel()
    {
        var formulationName = Formulations[0];
        var n = N[0];
        var k = K[0];
        var density = Densities[0];
        var realWeights = RealWeights.Contains(true);

        var formulation = GetFormulation(formulationName, k);

        using var env = new GRBEnv();
        env.LogToConsole = 0;
        // each run uses a single thread.
        env.Threads = 1;
        env.MIPGap = 0;
        env.TimeLimit = GurobiTimeLimit.TotalSeconds;
        env.Start();
        
        // run the formulation {_threads} times in parallel
        var runs =
            from seed in ParallelEnumerable.Range(42, _threads)
            let compat = ModelHelper.CreateCompatibility(n, density, realWeights, seed)
            let result = formulation.Run(env, compat.A, compat.w)
            select (seed, result);

        using var writer = new StreamWriter("output.dat", append: true);

        foreach (var run in runs)
            LogResult(writer, run.seed, run.result);
    }

    /// <summary>
    /// Returns the implementation of <see cref="GurobiFormulation"/> that corresponds to the specified
    /// <paramref name="name"/>.
    /// </summary>
    private static GurobiFormulation GetFormulation(string name, int k)
    {
        return name switch
        {
            "arcCycleRowGen" => new ArcCycleRowGenFormulation(k),
            "arcPath" => new ArcPathFormulation(k),
            "arcPathRowGen" => new ArcPathRowGenFormulation(k),
            "cycle" => new CycleFormulation(k),
            "edge" => new ExtendedEdgeFormulation(k),
            "mtz" => new MtzFormulation(k),
            _ => throw new ArgumentOutOfRangeException(nameof(name), name, "unknown formulation")
        };
    }

    /// <summary>
    /// Writes the current configuration and the specified <paramref name="result"/> to the specified
    /// <paramref name="writer"/>.
    /// </summary>
    private static void LogResult(TextWriter writer, int seed, Result result)
    {
        writer.WriteLine(
            "{0,-15} {1,4} {2,2} {3,2} {4,2} {5,2} {6,3} {7,1} {8,4} {9,4} {10,10} {11,10}",
            Formulations[0],
            N[0],
            K[0],
            (int)(100 * Densities[0]),
            0,
            99,
            seed,
            RealWeights.Contains(true) ? 1 : 0,
            (int)result.SetupTime.TotalSeconds,
            (int)result.RunningTime.TotalSeconds,
            MaxLength(result.Objective, 10),
            MaxLength(result.ObjectiveGap, 10));
        writer.Flush();
    }

    /// <summary>
    /// Returns a string representation of the specified <paramref name="number"/> that has no more than
    /// <paramref name="maxLength"/> characters.
    /// </summary>
    private static string MaxLength(double number, int maxLength)
    {
        string output = "";
        int decimalPlaces = maxLength - 2; //because every decimal contains at least "0."
        bool isError = true;

        while (isError && decimalPlaces >= 0)
        {
            output = Math.Round(number, decimalPlaces).ToString(NumberFormatInfo.InvariantInfo);
            isError = output.Length > maxLength;
            decimalPlaces--;
        }

        if (isError)
            throw new FormatException($"{number} can't be represented in {maxLength} characters");
        
        return output;
    }

    /// <summary>
    /// Starts one child process per configuration combination.
    /// </summary>
    private static void StartChildProcesses()
    {
        foreach (var n in N)
        foreach (var d in Densities)
        foreach (var k in K)
        foreach (var w in RealWeights)
        foreach (var formulation in Formulations)
            StartChildProcess(formulation, "n", n, "k", k, "d", d, "t", _threads, "w", w);
    }

    /// <summary>
    /// Starts Kep.Runner (this program) with the specified <paramref name="args"/>.
    /// </summary>
    private static void StartChildProcess(params object[] args)
    {
        Console.WriteLine($"{DateTime.Now:u}  Start  {String.Join(" ", args)}");
        var info = new ProcessStartInfo(Environment.ProcessPath);
        foreach (var arg in args)
        {
            if (arg is IFormattable f)
                info.ArgumentList.Add(f.ToString(null, CultureInfo.InvariantCulture));
            else
                info.ArgumentList.Add(arg.ToString()!);
        }

        // hide standard output and capture standard error
        info.UseShellExecute = false;
        info.RedirectStandardOutput = true;
        info.RedirectStandardError = true;

        var proc = new Process { StartInfo = info };

        try
        {
            proc.Start();
            proc.WaitForExit();

            var stderr = proc.StandardError.ReadToEnd();
            if (proc.ExitCode == 0 && String.IsNullOrEmpty(stderr))
                return;
            
            using var errorFile = new StreamWriter("error.dat", append: true);
            errorFile.WriteLine(String.Join(" ", args));
            errorFile.WriteLine("exit code: " + proc.ExitCode);
            errorFile.WriteLine(stderr);
            errorFile.WriteLine();
        }
        catch (Exception e)
        {
            using var errorFile = new StreamWriter("error.dat", append: true);
            errorFile.WriteLine(String.Join(" ", args));
            errorFile.WriteLine(e);
            errorFile.WriteLine();
        }
    }
}
