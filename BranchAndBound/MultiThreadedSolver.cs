namespace BranchAndBound;

public class MultiThreadedSolver(params INodeSelectionFactory[] selectionFactories)
{
    private readonly object _lock = new();

    public Task<TProblem?> SolveAsync<TProblem>(TProblem problem)
        where TProblem : IProblem<TProblem>
    {
        var root = new BasicNode<TProblem>(null, problem);
        root.TryClaim();
        root.Minimize();
        
        Console.WriteLine($"Initial : {root.Problem}");

        if (root.State == NodeState.Candidate)
            return Task.FromResult<TProblem?>(problem);
        
        if (root.State != NodeState.Relaxed)
            return Task.FromResult<TProblem?>(default);
        
        var selections = selectionFactories
            .Select(factory => factory.Create<BasicNode<TProblem>>()).ToArray();
        
        foreach (var selection in selections)
            selection.Update(root, root.Children);
        
        BasicNode<TProblem>? bestCandidate = null;
        int threadsRunning = selections.Length;
        bool isFinished = false;

        var threads = new Thread[selections.Length];
        for (var i = 0; i < selections.Length; i++)
        {
            var selection = selections[i];
            threads[i] = new Thread(() =>
            {
                while (true)
                {
                    var current = selection.TryClaimOne();
                    if (current == null)
                    {
                        lock (_lock)
                        {
                            threadsRunning--;

                            if (threadsRunning == 0)
                            {
                                isFinished = true;
                                Monitor.PulseAll(_lock);
                                return;
                            }

                            Monitor.Wait(_lock);

                            if (isFinished)
                                return;

                            threadsRunning++;
                        }

                        continue;
                    }

                    current.Minimize();

                    lock (_lock)
                    {
                        Console.WriteLine($"Thread {Environment.CurrentManagedThreadId}: {current.Problem}");
                        
                        if (current.State == NodeState.Candidate)
                        {
                            if (current.Objective <= root.LowerBound)
                            {
                                // Console.WriteLine("best candidate: " + current.Problem);

                                bestCandidate = current;
                                isFinished = true;

                                threadsRunning--;
                                Monitor.PulseAll(_lock);

                                return;
                            }

                            if (bestCandidate == null || current.Objective < bestCandidate.Objective)
                            {
                                // Console.WriteLine("best candidate: " + current.Problem);

                                bestCandidate = current;

                                root.Prune(current.Objective);
                            }
                            else
                            {
                                current.Prune(bestCandidate.Objective);
                            }

                            continue;
                        }

                        if (bestCandidate != null)
                        {
                            if (bestCandidate.Objective <= root.LowerBound)
                            {
                                isFinished = true;
                                threadsRunning--;
                                Monitor.PulseAll(_lock);
                                return;
                            }

                            current.Prune(bestCandidate.Objective);
                        }

                        if (current.State == NodeState.Relaxed)
                        {
                            foreach (var sel in selections)
                                sel.Update(current, current.Children);
                        }

                        Monitor.PulseAll(_lock);
                    }
                }
            });
            threads[i].Start();
        }

        foreach (var thread in threads)
            thread.Join();

        var bestCandidateProblem = bestCandidate == null ? default : bestCandidate.Problem;
        return Task.FromResult(bestCandidateProblem);
    }
}
