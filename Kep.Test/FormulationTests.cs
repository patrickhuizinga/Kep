using Gurobi;
using Kep.Runner;
using NUnit.Framework;

namespace Kep.Test;

public class FormulationTests
{
    [Test]
    [TestCase(10, 3, 0.2, 2.8962)]
    [TestCase(15, 3, 0.2, 4.5519)]
    [TestCase(20, 3, 0.2, 9.8199)]
    [TestCase(25, 3, 0.2, 13.3576)]
    [TestCase(30, 3, 0.2, 19.0218)]
    
    [TestCase(10, 3, 0.5, 5.7710)]
    [TestCase(20, 3, 0.5, 15.3730)]
    [TestCase(30, 3, 0.5, 24.3331)]
    
    [TestCase(10, 4, 0.2, 3.6748)]
    [TestCase(20, 4, 0.2, 11.5086)]
    [TestCase(30, 4, 0.2, 21.0538)]
    
    [TestCase(10, 4, 0.5, 5.8556)]
    [TestCase(20, 4, 0.5, 16.5220)]
    [TestCase(30, 4, 0.5, 25.4677)]
    
    [TestCase(30, 5, 0.2, 21.3232)]
    [TestCase(30, 5, 0.5, 26.1235)]
    [TestCase(40, 3, 0.5, 34.5825)]
    [TestCase(40, 5, 0.5, 36.2833)]
    public void ExtendedEdge(int n, int k, double density, double expectedObjective)
    {
        var (A, w) = CreateCompatibility(n, density, 42);
        
        var env = new GRBEnv();
        env.Start();
        var formulation = new ExtendedEdgeFormulation(k);
        var result = formulation.Run(env, A, w);
        var objective = result.Objective;
        
        Console.WriteLine("objective: " + objective);
        Assert.That(objective, Is.EqualTo(expectedObjective).Within(0.0001));
    }

    [Test]
    [TestCase(10, 3, 0.2, 2.8962)]
    [TestCase(15, 3, 0.2, 4.5519)]
    [TestCase(20, 3, 0.2, 9.8199)]
    [TestCase(25, 3, 0.2, 13.3576)]
    [TestCase(30, 3, 0.2, 19.0218)]
    
    [TestCase(10, 3, 0.5, 5.7710)]
    [TestCase(20, 3, 0.5, 15.3730)]
    [TestCase(30, 3, 0.5, 24.3331)]
    
    [TestCase(10, 4, 0.2, 3.6748)]
    [TestCase(20, 4, 0.2, 11.5086)]
    [TestCase(30, 4, 0.2, 21.0538)]
    
    [TestCase(10, 4, 0.5, 5.8556)]
    [TestCase(20, 4, 0.5, 16.5220)]
    [TestCase(30, 4, 0.5, 25.4677, Ignore = "~3 min")]
    
    [TestCase(30, 5, 0.2, 21.3232)]
    [TestCase(30, 5, 0.5, 26.1235)]
    [TestCase(40, 3, 0.5, 34.5825, Ignore = "takes many minutes")]
    [TestCase(40, 5, 0.5, 36.2833, Ignore = "takes many minutes")]
    public void Mtz(int n, int k, double density, double expectedObjective)
    {
        var (A, w) = CreateCompatibility(n, density, 42);
        
        var env = new GRBEnv();
        env.Start();
        var formulation = new MtzFormulation(k);
        var result = formulation.Run(env, A, w);
        var objective = result.Objective;
        
        Console.WriteLine("objective: " + objective);
        Assert.That(objective, Is.EqualTo(expectedObjective).Within(0.0001));
    }

    [Test]
    [TestCase(10, 3, 0.2, 2.8962)]
    [TestCase(15, 3, 0.2, 4.5519)]
    [TestCase(20, 3, 0.2, 9.8199)]
    [TestCase(25, 3, 0.2, 13.3576)]
    [TestCase(30, 3, 0.2, 19.0218)]
    
    [TestCase(10, 3, 0.5, 5.7710)]
    [TestCase(20, 3, 0.5, 15.3730)]
    [TestCase(30, 3, 0.5, 24.3331, Ignore = "> 3 min")]
    
    [TestCase(10, 4, 0.2, 3.6748)]
    [TestCase(20, 4, 0.2, 11.5086)]
    [TestCase(30, 4, 0.2, 21.0538)]
    
    [TestCase(10, 4, 0.5, 5.8556)]
    [TestCase(20, 4, 0.5, 16.5220)]
    [TestCase(30, 4, 0.5, 25.4677, Ignore = "takes many minutes")]
    
    [TestCase(30, 5, 0.2, 21.3232)]
    [TestCase(30, 5, 0.5, 26.1235)]
    [TestCase(40, 3, 0.5, 34.5825)]
    [TestCase(40, 5, 0.5, 36.2833)]
    public void ArcPath(int n, int k, double density, double expectedObjective)
    {
        var (A, w) = CreateCompatibility(n, density, 42);
        
        var env = new GRBEnv();
        env.Start();
        var formulation = new ArcPathFormulation(k);
        var result = formulation.Run(env, A, w);
        var objective = result.Objective;
        
        Console.WriteLine("objective: " + objective);
        Assert.That(objective, Is.EqualTo(expectedObjective).Within(0.0001));
    }

    [Test]
    [TestCase(10, 3, 0.2, 2.8962)]
    [TestCase(15, 3, 0.2, 4.5519)]
    [TestCase(20, 3, 0.2, 9.8199)]
    [TestCase(25, 3, 0.2, 13.3576)]
    [TestCase(30, 3, 0.2, 19.0218)]
    
    [TestCase(10, 3, 0.5, 5.7710)]
    [TestCase(20, 3, 0.5, 15.3730)]
    [TestCase(30, 3, 0.5, 24.3331)]
    
    [TestCase(10, 4, 0.2, 3.6748)]
    [TestCase(20, 4, 0.2, 11.5086)]
    [TestCase(30, 4, 0.2, 21.0538)]
    
    [TestCase(10, 4, 0.5, 5.8556)]
    [TestCase(20, 4, 0.5, 16.5220)]
    [TestCase(30, 4, 0.5, 25.4677)]
    
    [TestCase(30, 5, 0.2, 21.3232)]
    [TestCase(30, 5, 0.5, 26.1235)]
    [TestCase(40, 3, 0.5, 34.5825)]
    [TestCase(40, 5, 0.5, 36.2833)]
    public void ArcCycleRowGen(int n, int k, double density, double expectedObjective)
    {
        var (A, w) = CreateCompatibility(n, density, 42);
        
        var env = new GRBEnv();
        env.Start();
        var formulation = new ArcCycleRowGenFormulation(k);
        var result = formulation.Run(env, A, w);
        var objective = result.Objective;
        
        Console.WriteLine("objective: " + objective);
        Assert.That(objective, Is.EqualTo(expectedObjective).Within(0.0001));
    }

    [Test]
    [TestCase(10, 3, 0.2, 2.8962)]
    [TestCase(15, 3, 0.2, 4.5519)]
    [TestCase(20, 3, 0.2, 9.8199)]
    [TestCase(25, 3, 0.2, 13.3576)]
    [TestCase(30, 3, 0.2, 19.0218)]
    
    [TestCase(10, 3, 0.5, 5.7710)]
    [TestCase(20, 3, 0.5, 15.3730)]
    [TestCase(30, 3, 0.5, 24.3331)]
    
    [TestCase(10, 4, 0.2, 3.6748)]
    [TestCase(20, 4, 0.2, 11.5086)]
    [TestCase(30, 4, 0.2, 21.0538)]
    
    [TestCase(10, 4, 0.5, 5.8556)]
    [TestCase(20, 4, 0.5, 16.5220)]
    [TestCase(30, 4, 0.5, 25.4677)]
    
    [TestCase(30, 5, 0.2, 21.3232)]
    [TestCase(30, 5, 0.5, 26.1235)]
    [TestCase(40, 3, 0.5, 34.5825)]
    [TestCase(40, 5, 0.5, 36.2833)]
    public void ArcPathRowGen(int n, int k, double density, double expectedObjective)
    {
        var (A, w) = CreateCompatibility(n, density, 42);
        
        var env = new GRBEnv();
        env.Start();
        var formulation = new ArcPathRowGenFormulation(k);
        var result = formulation.Run(env, A, w);
        var objective = result.Objective;
        
        Console.WriteLine("objective: " + objective);
        Assert.That(objective, Is.EqualTo(expectedObjective).Within(0.0001));
    }

    [Test]
    [TestCase(10, 3, 0.2, 2.8962)]
    [TestCase(15, 3, 0.2, 4.5519)]
    [TestCase(20, 3, 0.2, 9.8199)]
    [TestCase(25, 3, 0.2, 13.3576)]
    [TestCase(30, 3, 0.2, 19.0218)]
    
    [TestCase(10, 3, 0.5, 5.7710)]
    [TestCase(20, 3, 0.5, 15.3730)]
    [TestCase(30, 3, 0.5, 24.3331)]
    
    [TestCase(10, 4, 0.2, 3.6748)]
    [TestCase(20, 4, 0.2, 11.5086)]
    [TestCase(30, 4, 0.2, 21.0538)]
    
    [TestCase(10, 4, 0.5, 5.8556)]
    [TestCase(20, 4, 0.5, 16.5220)]
    [TestCase(30, 4, 0.5, 25.4677)]
    
    [TestCase(30, 5, 0.2, 21.3232)]
    [TestCase(30, 5, 0.5, 26.1235)]
    [TestCase(40, 3, 0.5, 34.5825)]
    [TestCase(40, 5, 0.5, 36.2833)]
    public void Cycle(int n, int k, double density, double expectedObjective)
    {
        var (A, w) = CreateCompatibility(n, density, 42);
        
        var env = new GRBEnv();
        env.Start();
        var formulation = new CycleFormulation(k);
        var result = formulation.Run(env, A, w);
        var objective = result.Objective;
        
        Console.WriteLine("objective: " + objective);
        Assert.That(objective, Is.EqualTo(expectedObjective).Within(0.0001));
    }

    private static (bool[,] A, double[,] w) CreateCompatibility(int n, double density, int seed)
    {
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
            }

            if (rng.NextDouble() < density)
            {
                A[j, i] = true;
                w[j, i] = rng.NextDouble();
            }
        }

        return (A, w);
    }
}