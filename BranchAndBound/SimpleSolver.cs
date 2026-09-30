namespace BranchAndBound;

public class SimpleSolver(INodeSelectionFactory selectionFactory)
{
    public TProblem? Solve<TProblem>(TProblem problem)
        where TProblem : IProblem<TProblem>
    {
        var root = new BasicNode<TProblem>(null, problem);
        root.TryClaim();
        root.Minimize();
        
        if (root.State == NodeState.Candidate)
            return problem;
        
        if (root.State != NodeState.Relaxed)
            return default;
        
        var selection = selectionFactory.Create<BasicNode<TProblem>>();
        selection.Update(root, root.Children);

        BasicNode<TProblem>? bestCandidate = null;
        
        while (true)
        {
            var current = selection.TryClaimOne();
            if (current == null)
                break;
            
            current.Minimize();
            
            if (current.State == NodeState.Candidate)
            {
                if (current.Objective <= root.LowerBound)
                {
                    return current.Problem;
                }
                
                if (bestCandidate == null || current.Objective < bestCandidate.Objective)
                {
                    bestCandidate = current;
                    
                    root.Prune(bestCandidate.Objective);
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
                    break;
                
                current.Prune(bestCandidate.Objective);
            }

            if (current.State == NodeState.Relaxed)
                selection.Update(current, current.Children);
        }

        return bestCandidate == null ? default : bestCandidate.Problem;
    }
}