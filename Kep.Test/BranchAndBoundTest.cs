using System.Diagnostics;
using System.Globalization;
using BranchAndBound;
using Gurobi;
using Kep.MultiStage;
using Kep.MultiStage.BranchAndBound;
using Kep.Runner;
using Kep.Runner.Problem;
using NUnit.Framework;
using Glorie2014 = Kep.MultiStage.Glorie2014;

namespace Kep.Test;

[TestFixture]
public class BranchAndBoundTest
{
    private const double Epsilon = 0.0001;
    /// <summary>
    /// Magical value that generates a Saidman style compatibility matrix, instead of a purely random one.
    /// </summary>
    private const double SaidmanDensity = 9.9;
    
    private const int NumSolutions = 10;

    [Test]
    public void Knapsack()
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

        var problem = new GreedyKnapsackProblem();
        var solver = new SimpleSolver(
            new BestFirstNodeSelection());
        // new DepthFirstNodeSelection());
        // new ExhaustiveDepthFirstNodeSelection());
        // new BreadthFirstNodeSelection());
        var result1 = solver.Solve(problem);
        Console.WriteLine(result1 == null ? "<null>" : result1.ToString());
        Console.WriteLine();

        var solver2 = new MultiThreadedSolver(
            [
                new BestFirstNodeSelection(),
                new BestFirstNodeSelection(),
                new DepthFirstNodeSelection(),
                new BreadthFirstNodeSelection(),
            ]
        );
        var task = solver2.SolveAsync(problem);
        task.Wait();
        Console.WriteLine(task.Result == null ? "<null>" : task.Result.ToString());
        Console.WriteLine();

        var enumSolver = new SortedEnumerableSolver(new BestFirstNodeSelection());
        var results = enumSolver.SolveAll(problem);
        foreach (var item in results)
        {
            // results.TrySetUpperBound(item.Minimize().Objective + 1);
            Console.WriteLine(item.ToString())
                ;
        }
    }

    [Test]
    [TestCase(10, 3, 0.2, 2.8962)]
    [TestCase(10, 3, 0.5, 5.7710)]
    [TestCase(20, 3, 0.5, 15.3730)]
    [TestCase(30, 3, 0.5, 24.3331)]
    
    [TestCase(75, 3, 0.1, 38.8900)]
    [TestCase(75, 4, 0.1, 49.1471)]
    [TestCase(75, 5, 0.1, 54.0670, Ignore = "~1 minute")]

    [TestCase(99, 3, 0.1, 61.5787)]
    [TestCase(99, 4, 0.1, 72.5247, Ignore = "~3 minute")]
    
    [TestCase(99, 3, 0.3, 87.8495)]
    [TestCase(99, 4, 0.3, 90.0982, Ignore = "Takes too long")]
    
    // saidman graphs
    [TestCase(30, 3, SaidmanDensity, 8.71746)]
    [TestCase(30, 4, SaidmanDensity, 8.71746)]
    [TestCase(50, 3, SaidmanDensity, 20.2695)]
    [TestCase(50, 4, SaidmanDensity, 21.6492)]
    [TestCase(75, 3, SaidmanDensity, 29.6501)]
    [TestCase(75, 4, SaidmanDensity, 32.4222)]
    [TestCase(99, 3, SaidmanDensity, 41.8116)]
    [TestCase(99, 4, SaidmanDensity, 44.5377)]
    
    [TestCase(200, 3, SaidmanDensity, 100.4808)]
    public void Glorie(int n, int k, double density, double expectedObjective)
    {
        CultureInfo.CurrentCulture =  CultureInfo.InvariantCulture;
        Glorie2014.MasterProblem.MinimizeCount = 0;
        
        Console.WriteLine(DateTime.Now);
        var (A, w) = CreateCompatibility(n, density, 42);
        
        using var env = new GRBEnv();
        env.Threads = 1;
        env.Start();
        var sw = Stopwatch.StartNew();
        
        var form = new SortedEnumerableFormulation<Glorie2014.MasterProblem>(
            (a, w) => new Glorie2014.MasterProblem(env, a, w, k));

        var result = form.SolveMany(A, w, NumSolutions);

        var firstObjective = 0.0;
        int j = 0;
        foreach (var r in result)
        {
            var objective = r.objective;
            if (j == 0)
                firstObjective = objective;
            j++;
            Console.WriteLine($"{sw.Elapsed.TotalSeconds,7:F3}s {j,3}: {objective}");
        }

        Console.WriteLine("minimizes: " + Glorie2014.MasterProblem.MinimizeCount);
        Console.WriteLine("run time : " + sw.Elapsed);
        
        Assert.That(firstObjective, Is.EqualTo(expectedObjective).Within(Epsilon));
    }

    [Test]
    [TestCase(10, 3, 0.2, 2.8962)]
    [TestCase(10, 3, 0.5, 5.7710)]
    [TestCase(20, 3, 0.5, 15.3730)]
    [TestCase(30, 3, 0.5, 24.3331)]
    
    [TestCase(75, 3, 0.1, 38.8900)]
    [TestCase(75, 4, 0.1, 49.1471, Ignore = "~ 3 min")]
    [TestCase(75, 5, 0.1, 54.0670, Ignore = "takes too long")]

    [TestCase(99, 3, 0.1, 61.5787)]
    [TestCase(99, 4, 0.1, 72.5247, Ignore = "takes too long")]
    
    [TestCase(99, 3, 0.3, 87.8495, Ignore = "takes too long")]
    [TestCase(99, 4, 0.3, 90.0982, Ignore = "Takes too long")]
    
    // saidman graphs
    [TestCase(30, 3, SaidmanDensity, 8.71746)]
    [TestCase(30, 4, SaidmanDensity, 8.71746)]
    [TestCase(50, 3, SaidmanDensity, 20.2695)]
    [TestCase(50, 4, SaidmanDensity, 21.6492)]
    [TestCase(75, 3, SaidmanDensity, 29.6501)]
    [TestCase(75, 4, SaidmanDensity, 32.4222, Ignore = "> 2 min")]
    [TestCase(99, 3, SaidmanDensity, 41.8116)]
    [TestCase(99, 4, SaidmanDensity, 44.5377, Ignore = "takes too long")]
    public void Cycle(int n, int k, double density, double expectedObjective)
    {
        CultureInfo.CurrentCulture =  CultureInfo.InvariantCulture;
        CycleMasterProblem.MinimizeCount = 0;
        
        Console.WriteLine(DateTime.Now);
        var (A, w) = CreateCompatibility(n, density, 42);
        
        using var env = new GRBEnv();
        env.Threads = 1;
        env.Start();
        var sw = Stopwatch.StartNew();
        
        var form = new SortedEnumerableFormulation<CycleMasterProblem>(
            (a, w) => new CycleMasterProblem(env, a, w, k));

        var result = form.SolveMany(A, w, NumSolutions);

        var firstObjective = 0.0;
        int j = 0;
        foreach (var r in result)
        {
            var objective = r.objective;
            if (j == 0)
                firstObjective = objective;
            j++;
            Console.WriteLine($"{sw.Elapsed.TotalSeconds,7:F3}s {j,3}: {objective}");
        }

        Console.WriteLine("minimizes: " + CycleMasterProblem.MinimizeCount);
        Console.WriteLine("run time : " + sw.Elapsed);
        
        Assert.That(firstObjective, Is.EqualTo(expectedObjective).Within(Epsilon));
    }

    [Test]
    [TestCase(10, 3, 0.2, 2.8962)]
    [TestCase(10, 3, 0.5, 5.7710)]
    [TestCase(20, 3, 0.5, 15.3730)]
    [TestCase(30, 3, 0.5, 24.3331)]
    
    [TestCase(75, 3, 0.1, 38.8900)]
    [TestCase(75, 4, 0.1, 49.1471)]
    [TestCase(75, 5, 0.1, 54.0670)]

    [TestCase(99, 3, 0.1, 61.5787)]
    [TestCase(99, 4, 0.1, 72.5247)]
    
    [TestCase(99, 3, 0.3, 87.8495)]
    [TestCase(99, 4, 0.3, 90.0982, Ignore = "Takes too long")]
    
    // saidman graphs
    [TestCase(30, 3, SaidmanDensity, 8.71746)]
    [TestCase(30, 4, SaidmanDensity, 8.71746)]
    [TestCase(50, 3, SaidmanDensity, 20.2695)]
    [TestCase(50, 4, SaidmanDensity, 21.6492)]
    [TestCase(75, 3, SaidmanDensity, 29.6501)]
    [TestCase(75, 4, SaidmanDensity, 32.4222)]
    [TestCase(99, 3, SaidmanDensity, 41.8116)]
    [TestCase(99, 4, SaidmanDensity, 44.5377)]
    public void Cycle2(int n, int k, double density, double expectedObjective)
    {
        CultureInfo.CurrentCulture =  CultureInfo.InvariantCulture;
        CycleMasterProblem2.MinimizeCount = 0;
        
        Console.WriteLine(DateTime.Now);
        var (A, w) = CreateCompatibility(n, density, 42);
        
        using var env = new GRBEnv();
        env.Threads = 1;
        env.Start();
        var sw = Stopwatch.StartNew();
        
        var form = new SortedEnumerableFormulation<CycleMasterProblem2>(
            (a, w) => new CycleMasterProblem2(env, a, w, k));

        var result = form.SolveMany(A, w, NumSolutions);

        var firstObjective = 0.0;
        int j = 0;
        foreach (var r in result)
        {
            var objective = r.objective;
            if (j == 0)
                firstObjective = objective;
            j++;
            Console.WriteLine($"{sw.Elapsed.TotalSeconds,7:F3}s {j,3}: {objective}");
        }

        Console.WriteLine("minimizes: " + CycleMasterProblem2.MinimizeCount);
        Console.WriteLine("run time : " + sw.Elapsed);
        
        Assert.That(firstObjective, Is.EqualTo(expectedObjective).Within(Epsilon));
    }

    [Test]
    [TestCase(10, 3, 0.2, 2.8962)]
    [TestCase(10, 3, 0.5, 5.7710)]
    [TestCase(20, 3, 0.5, 15.3730)]
    [TestCase(30, 3, 0.5, 24.3331)]
    
    [TestCase(75, 3, 0.1, 38.8900)]
    [TestCase(75, 4, 0.1, 49.1471)]
    [TestCase(75, 5, 0.1, 54.0670)]

    [TestCase(99, 3, 0.1, 61.5787)]
    [TestCase(99, 4, 0.1, 72.5247)]
    
    [TestCase(99, 3, 0.3, 87.8495)]
    [TestCase(99, 4, 0.3, 90.0982, Ignore = "Takes too long")]
    
    // saidman graphs
    [TestCase(30, 3, SaidmanDensity, 8.71746)]
    [TestCase(30, 4, SaidmanDensity, 8.71746)]
    [TestCase(50, 3, SaidmanDensity, 20.2695)]
    [TestCase(50, 4, SaidmanDensity, 21.6492)]
    [TestCase(75, 3, SaidmanDensity, 29.6501)]
    [TestCase(75, 4, SaidmanDensity, 32.4222)]
    [TestCase(99, 3, SaidmanDensity, 41.8116)]
    [TestCase(99, 4, SaidmanDensity, 44.5377)]
    
    [TestCase(200, 3, SaidmanDensity, 100.4808)]
    public void Cycle3(int n, int k, double density, double expectedObjective)
    {
        CultureInfo.CurrentCulture =  CultureInfo.InvariantCulture;
        Console.WriteLine(DateTime.Now);
        var (A, w) = CreateCompatibility(n, density, 42);
        
        using var env = new GRBEnv();
        env.Threads = 1;
        env.Start();
        var sw = Stopwatch.StartNew();
        
        var problem = new CycleMultiFormulation(env, k);
        var result = problem.SolveMany(A, w, NumSolutions);

        var firstObjective = 0.0;
        int j = 0;
        foreach (var r in result)
        {
            var objective = r.objective;
            if (j == 0)
                firstObjective = objective;
            Console.WriteLine($"{sw.Elapsed.TotalSeconds,7:F3}s {++j,3}: {objective}");
        }

        Console.WriteLine("run time : " + sw.Elapsed);
        
        Assert.That(firstObjective, Is.EqualTo(expectedObjective).Within(Epsilon));
    }
    
    [Test]
    public void SaidmanInfo()
    {
        var numPairs = 1000;
        var (arcs, _) = CreateCompatibility(numPairs, SaidmanDensity, 42);
        Console.WriteLine(arcs.Indices().Count() / (1.0 *  numPairs * numPairs));
    }

    private static (bool[,] A, double[,] w) CreateCompatibility(int n, double density, int seed)
    {
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (density == SaidmanDensity)
            return CreateSaidmanCompatibility(n, seed);
        
        var A = new bool[n, n];
        var w = new double[n, n];
        var rng = new Random(seed);

        // create the array by start top left and then expand to the bottom right by adding 'rings'
        // that way you ensure a larger size always improves the objective
        for (int i = 0; i < n; i++)
        for (int j = 0; j < i; j++)
        {
            if (rng.NextDouble() < density)
            {
                A[i, j] = true;
                w[i, j] = rng.NextDouble();
                // w[i, j] = 1;
            }

            if (rng.NextDouble() < density)
            {
                A[j, i] = true;
                w[j, i] = rng.NextDouble();
                // w[j, i] = 1;
            }
        }

        return (A, w);
    }

    private static (bool[,] A, double[,] w) CreateSaidmanCompatibility(int n, int seed)
    {
        var rng =  new Random(seed);
        var rngW = new Random(seed + 1);
        
        var saidA = new SaidmanPoolGenerator(rng).Generate(n);
        var saidW = new double[n, n];
        
        // create the array by start top left and then expand to the bottom right by adding 'rings'
        // that way you ensure a larger size always improves the objective
        for (int i = 0; i < n; i++)
        for (int j = 0; j < i; j++)
        {
            if (saidA[i, j])
            {
                saidW[i, j] = rngW.NextDouble();
                // saidW[i, j] = 1;
            }

            if (saidA[j, i])
            {
                saidW[j, i] = rngW.NextDouble();
                // saidW[j, i] = 1;
            }
        }
            
        return (saidA, saidW);
    }
}