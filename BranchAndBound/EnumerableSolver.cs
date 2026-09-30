using System.Collections;

namespace BranchAndBound;

public class EnumerableSolver(INodeSelectionFactory selectionFactory)
{   
    public ProblemEnumerable<TProblem> SolveAll<TProblem>(TProblem problem)
        where TProblem : IProblem<TProblem>
    {
        return new ProblemEnumerable<TProblem>(selectionFactory, problem);
    }
    

    public class ProblemEnumerable<TProblem>(INodeSelectionFactory selectionFactory, TProblem problem) : IProblemEnumerable<TProblem>
        where TProblem : IProblem<TProblem>
    {
        private readonly Node<TProblem> _root = new(null, problem);
        
        public double UpperBound { get; private set; } = double.PositiveInfinity;

        public bool TrySetUpperBound(double upperBound)
        {
            if (UpperBound <= upperBound)
                return false;

            UpperBound = upperBound;
            _root.Prune(upperBound);
            
            return true;
        }

        public IEnumerator<TProblem> GetEnumerator()
        {
            _root.Minimize(UpperBound);

            if (_root.State == NodeState.Candidate)
                yield return problem;
            else if (_root.State != NodeState.Relaxed)
                yield break;

            var selection = selectionFactory.Create<Node<TProblem>>();
            selection.Update(_root, _root.Children);
        
            while (true)
            {
                var current = selection.TryClaimOne();
                if (current == null)
                    yield break;
            
                current.Minimize(UpperBound);

                if (current.State == NodeState.Candidate)
                    yield return current.Problem;

                selection.Update(current, current.Children);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    
    public class Node<TProblem>(Node<TProblem>? parent, TProblem problem) : INode
        where TProblem : IProblem<TProblem>
    {
        public INode? Parent => parent;

        public TProblem Problem => problem;

        public NodeState State { get; private set; } = NodeState.Initialized;

        public Node<TProblem>[] Children { get; private set; } = [];

        public double LowerBound { get; private set; } = parent?.LowerBound ?? double.NegativeInfinity;
    
        public double Priority => parent?.LowerBound ?? double.NegativeInfinity;

        public bool TryClaim()
        {
            if (State != NodeState.Initialized)
                return false;

            State = NodeState.Solving;
            return true;
        }

        public void Minimize(double upperBound)
        {
            var result = Problem.Minimize();
            
            LowerBound = result.LowerBound;
            
            // even if the result is a candidate, we prune it 
            if (upperBound < result.LowerBound)
            {
                State = NodeState.Pruned;
                return;
            }

            State = result.State;

            if (State == NodeState.Relaxed)
            {
                Children = Problem.Branch()
                    .Select(child => new Node<TProblem>(parent: this, child))
                    .ToArray();
                
                if (Children.Length == 0)
                    State = NodeState.Infeasible;
            }

            if (State == NodeState.Candidate)
            {
                Children = Problem.BranchCandidate()
                    .Select(child => new Node<TProblem>(parent: this, child))
                    .ToArray();
            }
        }

        public void Prune(double upperBound)
        {
            if (upperBound <= LowerBound)
            {
                State = NodeState.Pruned;
                Children = [];
            }

            foreach (var child in Children)
                child.Prune(upperBound);
        }
    }
}
