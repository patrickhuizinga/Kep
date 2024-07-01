namespace Kep.Runner;

/// <summary>
/// Represents a set of extension methods for arrays.
/// </summary>
public static class ArrayExtensions
{
    /// <summary>
    /// Returns <see cref="Array.Length"/>. Exists for consistency with its overloads.
    /// </summary>
    public static int Dim<T>(this T[] array) => array.Length;

    /// <summary>
    /// Returns the first and second dimensions of <paramref name="array"/>.
    /// </summary>
    public static (int, int) Dim<T>(this T[,] array) => (array.LengthI(), array.LengthJ());

    /// <summary>
    /// Returns the first, second and third dimensions of <paramref name="array"/>.
    /// </summary>
    public static (int, int, int) Dim<T>(this T[,,] array) => (array.LengthI(), array.LengthJ(), array.LengthK());

    /// <summary>
    /// Returns the first dimension of <paramref name="array"/>.
    /// </summary>
    public static int LengthI<T>(this T[,] array) => array.GetLength(0);

    /// <summary>
    /// Returns the second dimension of <paramref name="array"/>.
    /// </summary>
    public static int LengthJ<T>(this T[,] array) => array.GetLength(1);
    
    /// <summary>
    /// Returns the first dimension of <paramref name="array"/>.
    /// </summary>
    public static int LengthI<T>(this T[,,] array) => array.GetLength(0);
    
    /// <summary>
    /// Returns the second dimension of <paramref name="array"/>.
    /// </summary>
    public static int LengthJ<T>(this T[,,] array) => array.GetLength(1);
    
    /// <summary>
    /// Returns the third dimension of <paramref name="array"/>.
    /// </summary>
    public static int LengthK<T>(this T[,,] array) => array.GetLength(2);

    /// <summary>
    /// Returns all indices (i,j) where the specified <paramref name="array"/> contains true.
    /// </summary>
    public static IEnumerable<(int i, int j)> Indices(this bool[,] array)
    {
        var (lengthI, lengthJ) = array.Dim();

        for (int i = 0; i < lengthI; i++)
        for (int j = 0; j < lengthJ; j++)
        {
            if (array[i, j]) yield return (i, j);
        }
    }
}