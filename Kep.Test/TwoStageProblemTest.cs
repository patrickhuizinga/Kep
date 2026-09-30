using System.Globalization;
using Kep.MultiStage.TwoStage;
using Kep.Runner;
using Kep.Runner.Problem;
using NUnit.Framework;
using Problem = Kep.MultiStage.Problem;

namespace Kep.Test;

[TestFixture]
public class TwoStageProblemTest
{
    [Test]
    [TestCase(2, 2, 2,  47)]

    [TestCase(3, 2, 3, 42)]
    [TestCase(3, 2, 3, 43)]
    [TestCase(3, 2, 3, 44)]
    [TestCase(3, 2, 3, 45)]
    [TestCase(3, 2, 3, 46)]
    [TestCase(3, 2, 3, 47)]
    [TestCase(3, 2, 3, 48)]
    [TestCase(3, 2, 3, 49)]
    [TestCase(3, 2, 3, 50)]
    [TestCase(3, 2, 3, 51)]
    
    [TestCase(3, 3, 3, 42)]
    [TestCase(3, 3, 3, 43)]
    [TestCase(3, 3, 3, 44)]
    [TestCase(3, 3, 3, 45)]
    [TestCase(3, 3, 3, 46)]
    [TestCase(3, 3, 3, 47)]
    [TestCase(3, 3, 3, 48)]
    [TestCase(3, 3, 3, 49)]
    [TestCase(3, 3, 3, 50)]
    [TestCase(3, 3, 3, 51)]

    [TestCase(4, 4, 4, 47)]
    public void MipSecondStage50(int k1, int k2, int k3, int seed)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        
        var rng = new Random(seed);
        var pool = new SaidmanPoolGenerator(rng).GeneratePool(50);
        var arcs = pool.OptimisticCompatabilityMatrix;
        var weights = arcs.ToDouble();
        
        var secondStage = new MipSecondStageProblem(weights, k2, 3, 100);
        var problem = new Problem(weights, k1, 10, 10, secondStage);
        var objective = problem.Solve(pool, rng, CancellationToken.None);

        Console.WriteLine("cycle count: " + CycleFormulation.GetAllCycles(arcs, k1).Count());

        Console.WriteLine("best solution: " + objective);
    }

    [Test]
    [TestCase(3, 2, 3, 42)]
    [TestCase(3, 2, 3, 43)]
    [TestCase(3, 2, 3, 44)]
    [TestCase(3, 2, 3, 45)]
    [TestCase(3, 2, 3, 46)]
    [TestCase(3, 2, 3, 47)]
    [TestCase(3, 2, 3, 48)]
    [TestCase(3, 2, 3, 49)]
    [TestCase(3, 2, 3, 50)]
    [TestCase(3, 2, 3, 51)]
    
    [TestCase(3, 2, 3, 52)]
    [TestCase(3, 2, 3, 53)]
    [TestCase(3, 2, 3, 54)]
    [TestCase(3, 2, 3, 55)]
    [TestCase(3, 2, 3, 56)]
    [TestCase(3, 2, 3, 57)]
    [TestCase(3, 2, 3, 58)]
    [TestCase(3, 2, 3, 59)]
    [TestCase(3, 2, 3, 60)]
    [TestCase(3, 2, 3, 61)]
    
    [TestCase(2, 2, 3, 42)]
    [TestCase(2, 2, 3, 43)]
    [TestCase(2, 2, 3, 44)]
    [TestCase(2, 2, 3, 45)]
    [TestCase(2, 2, 3, 46)]
    [TestCase(2, 2, 3, 47)]
    [TestCase(2, 2, 3, 48)]
    [TestCase(2, 2, 3, 49)]
    [TestCase(2, 2, 3, 50)]
    [TestCase(2, 2, 3, 51)]
    
    [TestCase(2, 2, 3, 52)]
    [TestCase(2, 2, 3, 53)]
    [TestCase(2, 2, 3, 54)]
    [TestCase(2, 2, 3, 55)]
    [TestCase(2, 2, 3, 56)]
    [TestCase(2, 2, 3, 57)]
    [TestCase(2, 2, 3, 58)]
    [TestCase(2, 2, 3, 59)]
    [TestCase(2, 2, 3, 60)]
    [TestCase(2, 2, 3, 61)]
    public void MipSecondStage99(int k1, int k2, int k3, int seed)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        
        var rng = new Random(seed);
        var pool = new SaidmanPoolGenerator(rng).GeneratePool(99);
        var arcs = pool.OptimisticCompatabilityMatrix;
        var weights = arcs.ToDouble();
        
        var secondStage = new MipSecondStageProblem(weights, k2, k3, 100);
        var problem = new Problem(weights, k1, 100, 100, secondStage);
        var cts = new CancellationTokenSource(TimeSpan.FromHours(2));
        var objective = problem.Solve(pool, rng, cts.Token);

        Console.WriteLine("best solution: " + objective);
    }

    [Test]
    [TestCase(42)]
    [TestCase(43)]
    [TestCase(44)]
    [TestCase(45)]
    [TestCase(46)]
    [TestCase(47)]
    [TestCase(48)]
    [TestCase(49)]
    [TestCase(50)]
    [TestCase(51)]
  
    [TestCase(52)]
    [TestCase(53)]
    [TestCase(54)]
    [TestCase(55)]
    [TestCase(56)]
    [TestCase(57)]
    [TestCase(58)]
    [TestCase(59)]
    [TestCase(60)]
    [TestCase(61)]
    public void DutchSystemSimulation99(int seed)
    {
        const int scenarios = 100;
        int[] k = [2, 2, 2, 4];

        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        
        var rng = new Random(seed);
        var pool = new SaidmanPoolGenerator(rng).GeneratePool(99);
        var arcs = pool.OptimisticCompatabilityMatrix;
        var weights = arcs.ToDouble();

        //          k=[4,4]  k=[3,3]  k=[2,2]
        // seed 42 :  10.97    16.22    22.32    
        // seed 43 :  11.37    21.17    24.55    
        // seed 44 :  12.76    18.89    24.09    
        // seed 45 :  17.78    14.92    20.49    
        // seed 46 :  22.07    31.30    32.23    
        // seed 47 :   7.26    11.80    17.97    
        // seed 48 :  22.29    25.06    30.53    
        // seed 49 :  11.34    16.25    22.71    
        // seed 50 :  13.81    14.56    17.57    
        // seed 51 :  11.31    16.98    21.76    
        // seed 52 :  31.39    27.47    36.95    
        // seed 53 :  11.67    15.60    19.10    
        // seed 54 :  14.44    21.37    26.81    
        // seed 55 :  12.13    13.17    20.49    
        // seed 56 :   9.47    12.80    20.74    
        // seed 57 :  18.15    24.50    29.44    
        // seed 58 :  14.52    19.62    26.55    
        // seed 59 :  12.94    15.83    24.67    
        // seed 60 :  10.61    18.04    17.57    
        // seed 61 :  18.29    14.84    22.17    

        // seed 47
        // k = [4,4,4] -> 15+ (minutes)
        // k = [4,3,4] -> 18.35 (9 sec) 
        // k = [4,2,4] -> 19.76 (3 sec)
        // k = [3,3,4] -> 20.55 (6 sec)
        // k = [3,2,4] -> 22.60 (2 sec)
        // k = [2,2,4] -> 24.42 (1 sec)
        
        //   k = [3, 3, 4]
        // seed 42 :  25.41 ( 9   sec)
        // seed 43 :  39.79 ( 9   sec)
        // seed 44 :  30.81 ( 9   sec)
        // seed 45 :  14.26 ( 6   sec)
        // seed 46 :  37.27 (20   sec)
        // seed 47 :  20.55 ( 6   sec)
        // seed 48 :  33.60 (10   sec)
        // seed 49 :  26.85 ( 9   sec)
        // seed 50 :  23.13 (10   sec)
        // seed 51 :  26.35 ( 9   sec)
        // seed 52 :  41.21 (19   sec)
        // seed 53 :  22.37 (10   sec)
        // seed 54 :  32.93 (14   sec)
        // seed 55 :  20.12 ( 9   sec)
        // seed 56 :  24.34 (17   sec)
        // seed 57 :  37.02 (15   sec)
        // seed 58 :  33.34 (16   sec)
        // seed 59 :  25.96 (10   sec)
        // seed 60 :  24.37 (14   sec)
        // seed 61 :  26.11 (18   sec)

        //   k = [3, 2, 4]
        // seed 42, *100  solution:  30.36+ ( 1   sec)
        // seed 43, *100  solution:  33.74- ( 1   sec)
        // seed 44, *100  solution:  32.33+ ( 1   sec)
        // seed 45, *100  solution:  26.96+ ( 1   sec)
        // seed 46, *100  solution:  40.88  ( 1   sec)
        // seed 47, *100  solution:  20.60~ ( 1   sec)
        // seed 48, *100  solution:  38.65+ ( 1   sec)
        // seed 49, *100  solution:  29.62+ ( 2   sec)
        // seed 50, *100  solution:  26.90+ ( 1   sec)
        // seed 51, *100  solution:  30.55+ ( 3   sec)
        // seed 52, *100  solution:  37.15- (38   sec)
        // seed 53, *100  solution:  24.87+ ( 1   sec)
        // seed 54, *100  solution:  35.86+ ( 1   sec)
        // seed 55, *100  solution:  23.71  ( 1   sec)
        // seed 56, *100  solution:  28.85- ( 3   sec)
        // seed 57, *100  solution:  40.20+ ( 1   sec)
        // seed 58, *100  solution:  33.94  (14   sec)
        // seed 59, *100  solution:  31.23  ( 3   sec)
        // seed 60, *100  solution:  27.62+ ( 3   sec)
        // seed 61, *100  solution:  32.25+ ( 1   sec)

        //   k = [2, 2, 4]
        // seed 42, *100  solution:  29.17  ( 1   sec)
        // seed 43, *100  solution:  33.42- ( 1   sec)
        // seed 44, *100  solution:  31.44  ( 1   sec)
        // seed 45, *100  solution:  25.94  ( 1   sec)
        // seed 46, *100  solution:  41.69+ ( 1   sec)
        // seed 47, *100  solution:  24.42+ ( 1   sec)
        // seed 48, *100  solution:  38.88+ ( 1   sec)
        // seed 49, *100  solution:  29.70+ ( 1   sec)
        // seed 50, *100  solution:  23.67  ( 1   sec)
        // seed 51, *100  solution:  30.93+ ( 1   sec)
        // seed 52, *100  solution:  46.06  ( 1   sec)
        // seed 53, *100  solution:  25.14+ ( 1   sec)
        // seed 54, *100  solution:  35.82  ( 1   sec)
        // seed 55, *100  solution:  25.63+ ( 1   sec)
        // seed 56, *100  solution:  31.26  ( 1   sec)
        // seed 57, *100  solution:  39.11  ( 1   sec)
        // seed 58, *100  solution:  35.95+ ( 1   sec)
        // seed 59, *100  solution:  31.62+ ( 1   sec)
        // seed 60, *100  solution:  25.48  ( 1   sec)
        // seed 61, *100  solution:  30.72  ( 1   sec)

        // seed 55
        // k = [4,4,4,4] -> ?
        // k = [4,4,3,4] -> ?
        // k = [4,4,2,4] -> ?
        // k = [4,3,2,4] -> 28.956   82 sec
        // k = [3,3,2,4] -> 28.084  268 sec
        // k = [3,2,2,4] -> 28.505   48 sec

        //   k = [4, 3, 2, 4], scenarios=100*100
        // seed 42,  solution:  31.17  ( 20  min)
        // seed 43,  solution:  26.27  ( 66  min)
        // seed 44,  solution:  37.08  (  5  min)
        // seed 45,  solution:  30.31  (  2  min)
        // seed 46,  solution:  39.30  ( 43  min)
        // seed 47,  solution:  26.26  (  7  min)
        // seed 48,  solution:  44.32  (  1  min)
        // seed 49,  solution:  27.63  ( 61  min)
        // seed 50,  solution:  29.79  ( 17  min)
        // seed 51,  solution:  31.22  ( 38  min)
        // seed 52,  solution:  40.48  ( 60  min)
        // seed 53,  solution:  29.89  (  2  min)
        // seed 54,  solution:  37.16  ( 32  min)
        // seed 55,  solution:  28.96  (  1  min)
        // seed 56,  solution:  28.36  ( 58  min)
        // seed 57,  solution:  39.57  ( 48  min)
        // seed 58,  solution:  18.91  ( 93  min)
        // seed 59,  solution:  20.79  ( 84  min)
        // seed 60,  solution:  22.91  ( 66  min)
        // seed 61,  solution:  34.87  ( 26  min)

        //   k = [3, 2, 2, 4], scenarios=100*100
        // seed 42,  solution:  34.35  (  8  min)
        // seed 43,  solution:  37.61  ( 17  min)
        // seed 44,  solution:  37.00  (  2  min)
        // seed 45,  solution:  30.76  (  2  min)
        // seed 46,  solution:  47.80  (  1  min)
        // seed 47,  solution:  27.28  (  2  min)
        // seed 48,  solution:  43.61  (  3  min)
        // seed 49,  solution:  32.77  ( 13  min)
        // seed 50,  solution:  30.82  (  4  min)
        // seed 51,  solution:  34.83  ( 15  min)
        // seed 52,  solution:  37.35  ( 73  min)
        // seed 53,  solution:  29.50  (  5  min)
        // seed 54,  solution:  39.06  ( 25  min)
        // seed 55,  solution:  28.51  (  1  min)
        // seed 56,  solution:  20.33  ( 91  min)
        // seed 57,  solution:  46.45  ( 13  min)
        // seed 58,  solution:  30.55  ( 78  min)
        // seed 59,  solution:  32.34  ( 39  min)
        // seed 60,  solution:  32.46  (  5  min)
        // seed 61,  solution:  36.35  ( 19  min)

        //   k = [2, 2, 2, 4], scenarios=100*100
        // seed 42,  solution:  34.96  ( 45 sec)
        // seed 43,  solution:  37.98  ( 83 sec)
        // seed 44,  solution:  35.93  (104 sec)
        // seed 45,  solution:  29.84  ( 70 sec)
        // seed 46,  solution:  47.64  ( 48 sec)
        // seed 47,  solution:  27.05  ( 65 sec)
        // seed 48,  solution:  43.45  ( 39 sec)
        // seed 49,  solution:  33.40  ( 40 sec)
        // seed 50,  solution:  30.22  ( 44 sec)
        // seed 51,  solution:  35.15  (103 sec)
        // seed 52,  solution:  52.37  ( 58 sec)
        // seed 53,  solution:  29.26  ( 37 sec)
        // seed 54,  solution:  39.82  ( 89 sec)
        // seed 55,  solution:  28.10  ( 33 sec)
        // seed 56,  solution:  36.72  (133 sec)
        // seed 57,  solution:  45.78  ( 99 sec)
        // seed 58,  solution:  42.83  (100 sec)
        // seed 59,  solution:  36.69  ( 40 sec)
        // seed 60,  solution:  31.87  (106 sec)
        // seed 61,  solution:  35.92  ( 49 sec)
        
        SecondStageProblemBase secondStage;
        if (k.Length > 3)
        {
            var lastStage = new MipSecondStageProblem(weights, k[2], k[3], 1);
            secondStage = new MipIntermediateStageProblem(weights, k[1], 1, scenarios, lastStage, rng);
        }
        else
        {
            secondStage = new MipSecondStageProblem(weights, k[1], k[2], 1);
        }
        var problem = new Problem(weights, k[0], 1, scenarios, secondStage);
        var cancellation = new CancellationTokenSource(TimeSpan.FromHours(3));
        var objective = problem.Solve(pool, rng, cancellation.Token);

        Console.WriteLine("best solution: " + objective);
    }

    [Test]
    [TestCase(3, 42,  8.0367)]
    [TestCase(3, 43,  8.0367)]
    [TestCase(3, 44,  8.0367)]
    [TestCase(3, 45,  8.0367)]
    [TestCase(3, 46,  8.0367)]
    [TestCase(3, 47,  8.0367)]
    [TestCase(3, 48,  8.0367)]
    [TestCase(3, 49,  8.0367)]
    [TestCase(3, 50,  8.0367)]
    [TestCase(3, 51,  8.0367)]
    
    [TestCase(2, 47, 11.9056)]
    [TestCase(4, 47, 11.9056)]
    public void Random50(int k, int seed, double expectedObjective)
    {
        const int n = 50;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        
        var rng = new Random(seed);
        var pool = new SaidmanPoolGenerator(rng).GeneratePool(n);
        var arcs = pool.OptimisticCompatabilityMatrix;
        var weights = arcs.ToDouble();
        
        // seed 42,   10 attempts.  best solution:  12.45 (  2.0 sec)
        // seed 42,  100 attempts.  best solution:  13.54 ( 16.8 sec)
        // seed 42, 1000 attempts.  best solution:  14.10 (137   sec)
        
        // seed 43,  100 attempts.  best solution:  21.14 (260   sec)
        // seed 44,  100 attempts.  best solution:  16.49 ( 71   sec)
        // seed 45,  100 attempts.  best solution:  14.18 ( 32   sec)
        // seed 46,  100 attempts.  best solution:  17.22 (214   sec)
        
        // seed 47,   10 attempts.  best solution:  10.00 (  0.4 sec)
        // seed 47,  100 attempts.  best solution:  10.58 (  1.6 sec)
        // seed 47, 1000 attempts.  best solution:  10.79 ( 15.0 sec)
        // seed 47, 10K  attempts.  best solution:  10.94 (146   sec)
        
        // seed 48,  100 attempts.  best solution:  18.97 (  7.0 sec)
        // seed 49,  100 attempts.  best solution:  12.65 ( 18.6 sec)
        // seed 50,  100 attempts.  best solution:  10.70 (  1.6 sec)
        // seed 51,  100 attempts.  best solution:  14.99 ( 11.6 sec)

        var secondStage = new RandomSecondStageProblem(weights, k, 100);
        var problem = new Problem(weights, k, 10, 10, secondStage);
        var objective = problem.Solve(pool, rng, CancellationToken.None);

        Console.WriteLine("cycle count: " + CycleFormulation.GetAllCycles(arcs, k).Count());
        
        Console.WriteLine("best solution: " + objective);
        Console.WriteLine("cache hits: " + secondStage.CacheHits);
        Console.WriteLine("cache misses: " + secondStage.CacheMisses);


        // Assert.AreEqual(expectedObjective, objective, Epsilon);
    }

    [Test]
    [TestCase(3, 42, 11.9056)]
    [TestCase(3, 43, 11.9056)]
    [TestCase(3, 44, 11.9056)]
    [TestCase(3, 45, 11.9056)]
    [TestCase(3, 46, 11.9056)]
    [TestCase(3, 47, 11.9056)]
    [TestCase(3, 48, 11.9056)]
    [TestCase(3, 49, 11.9056)]
    [TestCase(3, 50, 11.9056)]
    [TestCase(3, 51, 11.9056)]
    
    [TestCase(2, 47, 11.9056)]
    public void Random99(int k, int seed, double expectedObjective)
    {
        const int n = 99;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        
        var rng = new Random(seed);
        var pool = new SaidmanPoolGenerator(rng).GeneratePool(n);
        var arcs = pool.OptimisticCompatabilityMatrix;
        var weights = arcs.ToDouble();
        
        // seed 42,   10 attempts.  best solution:  12.45 (  2.0 sec) *
        // seed 42,  100 attempts.  best solution:  13.54 ( 16.8 sec) *
        // seed 42, 1000 attempts.  best solution:  14.10 (137   sec) *
        
        // seed 43,  100 attempts.  best solution:  21.14 (260   sec) *
        // seed 44,  100 attempts.  best solution:  16.49 ( 71   sec) *
        // seed 45,  100 attempts.  best solution:  14.18 ( 32   sec) *
        // seed 46,  100 attempts.  best solution:  17.22 (214   sec) *
        
        // seed 47,   10 attempts.  best solution:  10.00 (  0.4 sec) *
        // seed 47,  100 attempts.  best solution:  10.58 (  1.6 sec) *
        // seed 47, 1000 attempts.  best solution:  10.79 ( 15.0 sec) *
        // seed 47, 10K  attempts.  best solution:  10.94 (146   sec) *
        
        // seed 48,  100 attempts.  best solution:  18.97 (  7.0 sec) *
        // seed 49,  100 attempts.  best solution:  12.65 ( 18.6 sec) *
        // seed 50,  100 attempts.  best solution:  10.70 (  1.6 sec) *
        // seed 51,  100 attempts.  best solution:  14.99 ( 11.6 sec) *
        
        
        // seed 47,  100 attempts.  best solution:  26.23 (  4.5 sec)  k=2

        var secondStage = new RandomSecondStageProblem(weights, k, 100);
        var problem = new Problem(weights, k, 10, 10, secondStage);
        var objective = problem.Solve(pool, rng, CancellationToken.None);

        Console.WriteLine("cycle count: " + CycleFormulation.GetAllCycles(arcs, k).Count());
        
        Console.WriteLine("best solution: " + objective);
        Console.WriteLine("cache hits: " + secondStage.CacheHits);
        Console.WriteLine("cache misses: " + secondStage.CacheMisses);


        // Assert.AreEqual(expectedObjective, objective, Epsilon);
    }

    [Test]
    [TestCase(3, 42)]
    [TestCase(3, 43)]
    [TestCase(3, 44)]
    [TestCase(3, 45)]
    [TestCase(3, 46)]
    [TestCase(3, 47)]
    [TestCase(3, 48)]
    [TestCase(3, 49)]
    [TestCase(3, 50)]
    [TestCase(3, 51)]
    
    [TestCase(2, 47)]
    public void WeightedRandom50(int k, int seed)
    {
        const int n = 50;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        
        var rng = new Random(seed);
        var pool = new SaidmanPoolGenerator(rng).GeneratePool(n);
        var arcs = pool.OptimisticCompatabilityMatrix;
        var weights = arcs.ToDouble();
        
        // seed 42,  100 attempts.  best solution:  13.32 (  3.8 sec)
        // seed 42, 1000 attempts.  best solution:  13.84 ( 26.0 sec)
        // seed 42, 10K  attempts.  best solution:  14.92 (388   sec)

        // seed 43, 1000 attempts.  best solution:  21.27 ( 35.9 sec)
        // seed 43, 10K  attempts.  best solution:  22.10 1846   sec)
        
        // seed 44, 1000 attempts.  best solution:  16.79 ( 29.4 sec)
        // seed 44, 10K  attempts.  best solution:  17.61 (879   sec)
        
        // seed 45, 1000 attempts.  best solution:  14.15 ( 26.2 sec)
        // seed 45, 10K  attempts.  best solution:  15.21 (546   sec)
        
        // seed 46, 1000 attempts.  best solution:  17.36 ( 40.9 sec)
        // seed 46, 10K  attempts.  best solution:  18.56 1690   sec)

        // seed 47,  100 attempts.  best solution:  10.08 (  1.5 sec) *
        // seed 47, 1000 attempts.  best solution:  10.61 ( 10.2 sec)
        // seed 47, 10K  attempts.  best solution:  11.07 (103   sec)

        // seed 48, 1000 attempts.  best solution:  18.98 ( 17.9 sec)
        // seed 48, 10K  attempts.  best solution:  19.67 (206   sec)
        
        // seed 49, 1000 attempts.  best solution:  12.60 ( 19.8 sec)
        // seed 49, 10K  attempts.  best solution:  13.40 (413   sec)
        
        // seed 50, 1000 attempts.  best solution:  11.27 ( 10.9 sec)
        // seed 50, 10K  attempts.  best solution:  12.35 (110   sec)
        
        // seed 51, 1000 attempts.  best solution:  14.83 ( 23.1 sec)
        // seed 51, 10K  attempts.  best solution:  15.66 (345   sec)
        
        
        // seed 47, 10K  attempts.  best solution:  10.78 ( 65   sec) k=2
        // seed 47, 10K  attempts.  best solution:  10.56 1020   sec) k=3, 100 scenarios
        // seed 47, 10K  attempts.  best solution:  11.33 1028   sec) k=3, 100 first stage sols

        var secondStage = new WeightedRandomSecondStageProblem(weights, k, 1_000, seed + 100);
        var problem = new Problem(weights, k, 10, 10, secondStage);
        var objective = problem.Solve(pool, rng, CancellationToken.None);

        Console.WriteLine("cycle count: " + CycleFormulation.GetAllCycles(arcs, k).Count());
        
        Console.WriteLine("best solution: " + objective);
        Console.WriteLine("cache hits: " + secondStage.CacheHits);
        Console.WriteLine("cache misses: " + secondStage.CacheMisses);
    }

    [Test]
    [TestCase(2, 47)]

    [TestCase(3, 42)]
    [TestCase(3, 43)]
    [TestCase(3, 44)]
    [TestCase(3, 45)]
    [TestCase(3, 46)]
    [TestCase(3, 47)]
    [TestCase(3, 48)]
    [TestCase(3, 49)]
    [TestCase(3, 50)]
    [TestCase(3, 51)]
    public void WeightedRandom99(int k, int seed)
    {
        const int n = 99;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        
        var rng = new Random(seed);
        var pool = new SaidmanPoolGenerator(rng).GeneratePool(n);
        var arcs = pool.OptimisticCompatabilityMatrix;
        var weights = arcs.ToDouble();
        
        // seed 42,  100 attempts.  best solution:  13.32 (  3.8 sec) *
        // seed 42, 1000 attempts.  best solution:  27.78 1209   sec)
        // seed 42, 10K  attempts.  best solution:  14.92 (388   sec) *
        
        // seed 43, 1000 attempts.  best solution:  28.90 9000   sec)
        // seed 43, 10K  attempts.  best solution:  22.10 1846   sec) *
        
        // seed 44, 1000 attempts.  best solution:  31.06 8640   sec)
        // seed 44, 10K  attempts.  best solution:  17.61 (879   sec) *
        
        // seed 45, 1000 attempts.  best solution:  22.77 (834   sec)
        // seed 45, 1000 attempts.  best solution:  15.21 (546   sec) *
        
        // seed 46, 1000 attempts.  best solution:  41.23 6720   sec)
        // seed 46, 10K  attempts.  best solution:  18.56 1690   sec) *
        
        // seed 47,  100 attempts.  best solution:  18.99 ( 44.2 sec)
        // seed 47, 1000 attempts.  best solution:  19.66 (259   sec)
        // seed 47, 10K  attempts.  best solution:  11.07 (103   sec) *
        
        // seed 48, 1000 attempts.  best solution:  34.70 1871   sec)
        // seed 48, 10K  attempts.  best solution:  19.67 (206   sec) *
        
        // seed 49, 1000 attempts.  best solution:  25.37 (430   sec)
        // seed 49, 10K  attempts.  best solution:  13.40 (413   sec) *
        
        // seed 50, 1000 attempts.  best solution:  11.27 ( 10.9 sec) *
        // seed 50, 10K  attempts.  best solution:  12.35 (110   sec) *
        
        // seed 51, 1000 attempts.  best solution:  14.83 ( 23.1 sec) *
        // seed 51, 10K  attempts.  best solution:  15.66 (345   sec) *
        
        
        // seed 47, 1000 attempts.  best solution:  26.74 ( 43.9 sec) k=2
        // seed 47, 10K  attempts.  best solution:  27.21 (430   sec) k=2
        // seed 47, 10K  attempts.  best solution:  10.56 1020   sec) k=3, 100 scenarios *
        // seed 47, 10K  attempts.  best solution:  11.33 1028   sec) k=3, 100 first stage sols *

        var secondStage = new WeightedRandomSecondStageProblem(weights, k, 1_000, seed + 100);
        var problem = new Problem(weights, k, 10, 10, secondStage);
        var objective = problem.Solve(pool, rng, CancellationToken.None);

        Console.WriteLine("cycle count: " + CycleFormulation.GetAllCycles(arcs, k).Count());
        
        Console.WriteLine("best solution: " + objective);
        Console.WriteLine("cache hits: " + secondStage.CacheHits);
        Console.WriteLine("cache misses: " + secondStage.CacheMisses);
    }
}







