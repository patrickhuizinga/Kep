using System.Globalization;
using Gurobi;
using Kep.MultiStage.TwoStage;
using Kep.Runner;
using Kep.Runner.Problem;
using NUnit.Framework;

namespace Kep.Test;

[TestFixture]
public class ExpectationFormulationTest
{
    private const double Epsilon = 0.1;

    [Test]
    [TestCase(5, 3,  1.54398)]
    [TestCase(6, 3,  3.50964)]
    [TestCase(7, 3,  4.08763)]
    [TestCase(8, 3,  4.08763)]
    [TestCase(9, 3,  5.06377)]
    [TestCase(10, 3,  5.11965)]
    [TestCase(11, 3,  5.18642)]
    [TestCase(12, 3,  5.21777)]
    [TestCase(13, 3,  5.23919)]
    [TestCase(14, 3,  5.25257)]
    [TestCase(15, 3,  5.32909)]
    [TestCase(16, 3,  5.32909)]
    
    [TestCase( 5, 4,  1.54398)]
    [TestCase( 6, 4,  3.50964)]
    [TestCase( 7, 4,  4.08763)]
    [TestCase( 8, 4,  4.08763)]
    [TestCase( 9, 4,  5.39269)]
    [TestCase(10, 4,  5.44736)]
    [TestCase(11, 4,  5.49903)]
    [TestCase(12, 4,  5.52245)]
    [TestCase(13, 4,  5.54347)]
    [TestCase(14, 4,  5.55312)]
    [TestCase(15, 4,  5.63875)]
    public void Test(int n, int k, double expectedObjective)
    {
        using var env = new GRBEnv();
        env.LogToConsole = 0;
        env.OutputFlag = 0;
        env.Threads = 1;
        env.Start();

        var rng = new Random(47);
        var pool = new SaidmanPoolGenerator(rng).GeneratePool(n);
        var arcs = pool.OptimisticCompatabilityMatrix;
        Console.WriteLine("arcs: " + arcs.Indices().Count());
        
        var weights = arcs.ToDouble();
        
        var form = new ExpectationFormulation(weights);
        var objective = form.Run(arcs, k, pool, env, CancellationToken.None);

        Console.WriteLine("cache hits:  " + form.CacheHits);
        Console.WriteLine("cache misses: " + form.CacheMisses);

        Assert.AreEqual(expectedObjective, objective, Epsilon);
    }
}
