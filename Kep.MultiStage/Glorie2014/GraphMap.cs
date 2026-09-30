using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Kep.MultiStage.Glorie2014;

/// <summary>
/// Represents a mapping from a graph to some value. 
/// </summary>
public class GraphMap<T>
{
    private readonly Node _root = new();
    private readonly Dictionary<SortedArcs, T> _dict = new(1000);

    private readonly List<List<int>> _arcs = [];
    
    public ref T? GetOrAddDefault(IEnumerable<int[]> cycles, int n)
    {
        ResetArcs(n);

        var arcs = _arcs;
        foreach (var cycle in cycles)
        {
            AddCycle(cycle, arcs);
        }
        
        return ref GetOrAddDefault(arcs);
    }

    public ref T? GetOrAddDefault2(List<int[]> cycles, int n)
    {
        ResetArcs(n);

        var arcs = _arcs;

        var span = CollectionsMarshal.AsSpan(cycles);
        foreach (var cycle in span)
        {
            AddCycle(cycle, arcs);
        }

        return ref GetOrAddDefault(arcs);
    }

    private void ResetArcs(int n)
    {
        var arcs = _arcs;
        foreach (var arc in arcs)
        {
            arc.Clear();
        }

        for (int i = arcs.Count; i < n; i++)
        {
            arcs.Add(new List<int>(n));
        }
    }

    private static void AddCycle(int[] cycle, List<List<int>> arcs)
    {
        var u = cycle[^1];
        foreach (var v in cycle)
        {
            arcs[u].Add(v);
                
            u = v;
        }
    }
    

    public ref T? GetOrAddDefault(List<int[]> cycles)
    {
        var arcCount = 0;
        foreach (var cycle in cycles)
        {
            arcCount += cycle.Length;
        }
        
        var bitArcs = new uint[arcCount];
        
        int i = 0;
        foreach (var cycle in cycles)
        {
            var u = cycle[^1];
            foreach (var v in cycle)
            {
                bitArcs[i++] = (uint)u << 16 | (uint)v;
                u = v;
            }
        }
        //bitArcs.Sort();
        
        return ref GetOrAddDefault(bitArcs);
    }

    public ref T? GetOrAddDefault(IEnumerable<(int u, int v)> arcs, int n)
    {
        bool[]?[] hasArc = new bool[n][];
        
        foreach (var arc in arcs)
        {
            ref var row = ref hasArc[arc.u];
            if (row == null)
                row = new bool[n];
                
            row[arc.v] = true;
        }

        return ref GetOrAddDefault(hasArc);
    }

    public ref T? GetOrAddDefault(IEnumerable<uint> bitArcs, int n)
    {
        bool[]?[] hasArc = new bool[n][];
        
        foreach (var bitArc in bitArcs)
        {
            var u = (int)(bitArc >> 16);
            var v = (int)(bitArc & 0xFFFF);
            
            ref var row = ref hasArc[u];
            if (row == null)
                row = new bool[n];
                
            row[v] = true;
        }

        return ref GetOrAddDefault(hasArc);
    }

    private ref T? GetOrAddDefault(bool[]?[] hasArc)
    {
        throw new InvalidOperationException();
        var current = _root;
        
        for (int u = 0; u < hasArc.Length; u++)
        {
            var row = hasArc[u];
            if (row == null)
                continue;

            for (int v = 0; v < row.Length; v++)
            {
                if (row[v])
                {
                    var bitArc = (uint)u << 16 | (uint)v;
                    current = current.GetOrCreateChild(bitArc);
                }
            }
        }
        
        return ref current.Item;
    }

    private ref T? GetOrAddDefault(uint[] bitArcs)
    {
        return ref CollectionsMarshal.GetValueRefOrAddDefault(_dict, new SortedArcs(bitArcs), out var exists);
        
        throw new InvalidOperationException();
        var current = _root;

        foreach (var bitArc in bitArcs)
            current = current.GetOrCreateChild(bitArc);
        
        return ref current.Item;
    }

    private ref T? GetOrAddDefault(List<List<int>> arcs)
    {
        throw new InvalidOperationException();
        var current = _root;

        for (var u = 0; u < arcs.Count; u++)
        {
            var row = arcs[u];
            row.Sort();
            
            foreach (var v in row)
            {
                var bitArc = (uint)u << 16 | (uint)v;
                current = current.GetOrCreateChild(bitArc);
            }
        }

        return ref current.Item;
    }

    
    private class Node
    {
        internal T? Item;
        
        private const int CutoverPoint = 8;
        private const int InitialHashtableSize = 13;
    
        private uint[]? _keys;
        private Node[]? _values;
        private int _count;
        private Dictionary<uint, Node>? _hashtable;
    
        public Node GetOrCreateChild(uint key)
        {
            if (_hashtable == null)
            {
                if (_keys == null)
                {
                    _keys = new uint[CutoverPoint];
                    _values = new Node[CutoverPoint];
                    
                    _keys[0] = key;
                    _count = 1;
                    return _values[0] = new Node();
                }
                
                Debug.Assert(_keys != null);
                Debug.Assert(_values != null);

                for (var i = 0; i < _count; i++)
                {
                    if (_keys[i] == key)
                        return _values[i];
                }
    
                if (_count < CutoverPoint)
                {
                    _keys[_count] = key;
                    // _values[_count] = new Node();
                    return _values[_count++] = new Node();
                }
    
                ChangeOver();
                Debug.Assert(_hashtable != null);
            }
    
            ref var child = ref CollectionsMarshal.GetValueRefOrAddDefault(_hashtable, key, out var exists);
            if (!exists)
                child = new Node();

            return child!;
        }
    
        private void ChangeOver()
        {
            Debug.Assert(_keys != null);
            Debug.Assert(_values != null);
        
            _hashtable = new Dictionary<uint, Node>(InitialHashtableSize);
    
            for (var i = 0; i < _count; i++)
                _hashtable.Add(_keys[i], _values[i]);
    
            _keys = null;
            _values = null;
        }
    }
    
    private readonly struct SortedArcs(uint[] arcs) : IEquatable<SortedArcs>
    {
        public readonly uint[] Arcs = arcs;

        public bool Equals(SortedArcs other)
        {
            // if (Arcs.Length != other.Arcs.Length)
            //     return false;

            return Arcs.SequenceEqual(other.Arcs);
            // Span<uint> span = Arcs;
            // Span<uint> otherSpan = other.Arcs;
            // for (var i = 0; i < span.Length; i++)
            // {
            //     if (span[i] != otherSpan[i])
            //         return false;
            // }
            //
            // return true;
        }

        public override bool Equals(object? obj)
        {
            return obj is SortedArcs other && Equals(other);
        }

        public override int GetHashCode()
        {
            int hash = 0;
            Span<uint> span = Arcs;
            foreach (var arc in span)
            {
                hash = hash * 31 + arc.GetHashCode();
            }
            return hash;
        }
    }
}
