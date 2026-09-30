using System.Collections;

namespace BranchAndBound;

public class SortedEnumerableSolver(INodeSelectionFactory selectionFactory)
{
    private const double Tolerance = 0.0001;
    
    public ProblemEnumerable<TProblem> SolveAll<TProblem>(TProblem problem)
        where TProblem : IProblem<TProblem>
    {
        return new ProblemEnumerable<TProblem>(selectionFactory, problem);
    }
    

    public class ProblemEnumerable<TProblem>(INodeSelectionFactory selectionFactory, TProblem problem) : IProblemEnumerable<TProblem>
        where TProblem : IProblem<TProblem>
    {
        private readonly Node<TProblem> _root = Node<TProblem>.Root(problem);
        
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
            _root.Minimize(UpperBound + Tolerance);

            if (_root.State == NodeState.Candidate)
                yield return problem;
            else if (_root.State != NodeState.Relaxed)
                yield break;

            var queue = new PriorityQueue<Node<TProblem>, double>();
            
            var selection = selectionFactory.Create<Node<TProblem>>();
            selection.Update(_root, _root.Children);
        
            while (true)
            {
                while (queue.TryPeek(out var topProblem, out var topObjective))
                {
                    if (_root.RelaxedLowerBound + Tolerance < topObjective)
                        break;

                    queue.Dequeue();
                    yield return topProblem.Problem;
                }
                
                var current = selection.TryClaimOne();
                if (current == null)
                    break;
            
                current.Minimize(UpperBound + Tolerance);

                if (current.State == NodeState.Candidate)
                    queue.Enqueue(current, current.Objective!.Value);

                selection.Update(current, current.Children);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    
    public class Node<TProblem> : INode
        where TProblem : IProblem<TProblem>
    {
        private Node(TProblem problem)
        {
            Parent = null;
            Problem = problem;
            LowerBound = double.NegativeInfinity;
            RelaxedLowerBound = double.NegativeInfinity;
        }

        private Node(Node<TProblem> parent, TProblem problem)
        {
            Parent = parent;
            Problem = problem;
            LowerBound = parent.LowerBound;
            RelaxedLowerBound = parent.RelaxedLowerBound;
        }

        public static Node<TProblem> Root(TProblem problem) => new(problem);

        
        public Node<TProblem>? Parent { get; }

        INode? INode.Parent => Parent;

        public TProblem Problem { get; }

        public NodeState State { get; private set; } = NodeState.Initialized;

        public Node<TProblem>[] Children { get; private set; } = [];

        public double? Objective { get; private set; }
        public double LowerBound { get; private set; }
        
        /// <summary>
        /// The best lower bound of any child, but only including the relaxed nodes.
        /// This property gives a floor to any objective value that can still be found.
        /// </summary>
        public double RelaxedLowerBound { get; private set; }
    
        public double Priority => LowerBound;

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
            
            Objective = result.Objective;
            LowerBound = result.LowerBound;
            RelaxedLowerBound = result.LowerBound;

            // even if the result is a candidate, we prune it 
            if (upperBound < result.LowerBound)
            {
                State = NodeState.Pruned;
                Parent?.UpdateLowerBound();
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
            else if (State == NodeState.Candidate)
            {
                Children = Problem.BranchCandidate()
                    .Select(child => new Node<TProblem>(parent: this, child))
                    .ToArray();
            }
            
            UpdateLowerBound();
            Parent?.UpdateLowerBound();
        }

        /// <summary>
        /// Sets the lower bound to the smallest of the children's lower bounds.
        /// </summary>
        private void UpdateLowerBound()
        {
            // deal with the case of 0 children, for example when this is not a relaxed node
            if (Children.Length == 0)
            {
                RelaxedLowerBound =  double.PositiveInfinity;
                Parent?.UpdateLowerBound();
                return;
            }
            
            var min = Children.Min(n => n.LowerBound);
            var minRelaxed = Children.Min(n => n.RelaxedLowerBound);
            
            // initially a child's lower bound is set to the parent's lower bound,
            // so if the best child is still equal (or less), then we don't need to update ours, nor our parent's
            // no child's LB should ever be less, but rounding errors can cause this to happen
            if (min <= LowerBound && minRelaxed <= RelaxedLowerBound)
                return;
            
            LowerBound = min;
            RelaxedLowerBound = minRelaxed;
            Parent?.UpdateLowerBound();
        }

        public void Prune(double upperBound)
        {
            if (upperBound <= LowerBound)
            {
                State = NodeState.Pruned;
                Children = [];
                RelaxedLowerBound = double.PositiveInfinity;
            }

            foreach (var child in Children)
                child.Prune(upperBound);
        }
    }
}
