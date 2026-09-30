namespace Kep.Runner.Problem;

public sealed class Pool(List<PatientDonorPair> pairs, bool?[,] knownMatches)
{
    public int Size => pairs.Count;
    
    public List<PatientDonorPair> Pairs => pairs;
    
    /// <summary>
    /// Returns the compatability matrix as it is known so far.
    /// </summary>
    /// <remarks>
    /// By default, entries are false when a combination is not blood-compatible.
    ///
    /// Can be modified directly, or through <see cref="GetOrDrawIsMatch"/> or <see cref="GetOrDrawAllMatches"/>.
    /// </remarks>
    public bool?[,] KnownMatches => knownMatches;

    public Pool(List<PatientDonorPair> pairs)
        : this(pairs, InitializeKnowMatches(pairs))
    {
    }

    private static bool?[,] InitializeKnowMatches(List<PatientDonorPair> pairs)
    {
        var result = new bool?[pairs.Count, pairs.Count];
        for (int u = 0; u < pairs.Count; u++)
        for (int v = 0; v < pairs.Count; v++)
        {
            if (u == v)
                result[u, v] = false;
            else if (!pairs[u].CanDonateTo(pairs[v]))
                result[u, v] = false;
        }
        return result;
    }

    public bool[,] OptimisticCompatabilityMatrix
    {
        get
        {
            var result = new bool[pairs.Count, pairs.Count];
            for (var u = 0; u < pairs.Count; u++)
            for (var v = 0; v < pairs.Count; v++)
            {
                // if we don't know for sure, then it might be possible
                result[u, v] = knownMatches[u, v] ?? true;
            }

            return result;
        }
    }

    public bool[,] PessimisticCompatabilityMatrix
    {
        get
        {
            var result = new bool[pairs.Count, pairs.Count];
            for (var u = 0; u < pairs.Count; u++)
            for (var v = 0; v < pairs.Count; v++)
            {
                // if we don't know for sure, then consider it impossible
                result[u, v] = knownMatches[u, v] ?? false;
            }

            return result;
        }
    }
    
    /// <summary>
    /// Returns 1.0 if the pair is known to be a match, 0.0 if they are known not to be a match,
    /// otherwise 1.0 - the patient's CPRA.
    /// </summary>
    public double GetCompatibility(int u, int v)
    {
        return knownMatches[u, v].ToDouble()
            ?? (1 - pairs[u].PatientCpra);
    }

    public bool GetOrDrawIsMatch(int u, int v, Random rng)
    {
        return knownMatches[u, v] ??= !DrawIsPositiveCrossmatch(pairs[u].PatientCpra, rng);
    }

    public bool[,] GetOrDrawAllMatches(Random rng)
    {
        var result = new bool[pairs.Count, pairs.Count];
        for (var u = 0; u < pairs.Count; u++)
        for (var v = 0; v < pairs.Count; v++)
        {
            result[u, v] = knownMatches[u, v] ??= !DrawIsPositiveCrossmatch(pairs[u].PatientCpra, rng);
        }
        return result;
    }

    private static bool DrawIsPositiveCrossmatch(double cpra, Random rng)
    {
        return rng.NextDouble() <= cpra;
    }
    
    public void DisablePair(int index)
    {
        for (int j = 0; j < Size; j++)
        {
            knownMatches[index, j] = false;
            knownMatches[j, index] = false;
        }
    }

    public Pool Clone()
    {
        return new Pool(pairs, (bool?[,])knownMatches.Clone());
    }
}
