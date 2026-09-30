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

    
    extension(bool[] array)
    {
        /// <summary>
        /// Returns all indices where the specified <paramref name="array"/> contains true.
        /// </summary>
        public IEnumerable<int> Indices()
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i])
                    yield return i;
            }
        }
    }

    
    extension(bool[,] array)
    {
        /// <summary>
        /// Returns all indices (i,j) where the specified <paramref name="array"/> contains true.
        /// </summary>
        public IEnumerable<(int i, int j)> Indices()
        {
            var (lengthI, lengthJ) = array.Dim();

            for (int i = 0; i < lengthI; i++)
            for (int j = 0; j < lengthJ; j++)
            {
                if (array[i, j]) yield return (i, j);
            }
        }

        /// <summary>
        /// Returns an array of doubles where each element is 1 if the corresponding element in
        /// <paramref name="array"/> is true, and 0 otherwise.
        /// </summary>
        public double[,] ToDouble()
        {
            var (lengthI, lengthJ) = array.Dim();
        
            var result = new double[lengthI, lengthJ];
            for (int i = 0; i < lengthI; i++)
            for (int j = 0; j < lengthJ; j++)
                result[i, j] = array[i, j] ? 1 : 0;

            return result;
        }

        /// <summary>
        /// Returns the result of summing each row. <br/>
        /// result[i] = sum_j array[i,j] <br/>
        /// result = array dot onesVec
        /// </summary>
        public int[] SumPerI()
        {
            var result = new int[array.LengthI()];
            for (int i = 0; i < array.LengthI(); i++)
            for (int j = 0; j < array.LengthJ(); j++)
                result[i] += array[i, j] ? 1 : 0;

            return result;
        }

        /// <summary>
        /// Returns the result of summing each column.
        /// result[j] = sum_i array[i,j]
        /// result = onesVec dot array
        /// </summary>
        public int[] SumPerJ()
        {
            var result = new int[array.LengthJ()];
            for (int i = 0; i < array.LengthI(); i++)
            for (int j = 0; j < array.LengthJ(); j++)
                result[j] += array[i, j] ? 1 : 0;

            return result;
        }

        public static double[,] operator *(bool[,] arr, double factor)
        {
            var result = new double[arr.LengthI(), arr.LengthJ()];
            for (int i = 0; i < arr.LengthI(); i++)
            for (int j = 0; j < arr.LengthJ(); j++)
                result[i, j] = arr[i, j]? factor : 0;
            return result;
        }

        public static double[,] operator *(double factor, bool[,] arr)
        {
            var result = new double[arr.LengthI(), arr.LengthJ()];
            for (int i = 0; i < arr.LengthI(); i++)
            for (int j = 0; j < arr.LengthJ(); j++)
                result[i, j] = arr[i, j]? factor : 0;
            return result;
        }
    }

    
    extension(double[,] array)
    {
        /// <summary>
        /// Returns the result of summing each row.
        /// result[i] = sum_j array[i,j]
        /// result = array dot onesVec
        /// </summary>
        public double[] SumPerI()
        {
            var result = new double[array.LengthI()];
            for (int i = 0; i < array.LengthI(); i++)
            for (int j = 0; j < array.LengthJ(); j++)
                result[i] += array[i, j];

            return result;
        }

        /// <summary>
        /// Returns the result of summing each column.
        /// result[j] = sum_i array[i,j]
        /// result = onesVec dot array
        /// </summary>
        public double[] SumPerJ()
        {
            var result = new double[array.LengthJ()];
            for (int i = 0; i < array.LengthI(); i++)
            for (int j = 0; j < array.LengthJ(); j++)
                result[j] += array[i, j];

            return result;
        }

        /// <summary>
        /// Returns the maximum of each row.
        /// </summary>
        public double[] MaxPerI()
        {
            var result = new double[array.LengthI()];
            for (int i = 0; i < array.LengthI(); i++)
            {
                var max = double.MinValue;
                for (int j = 0; j < array.LengthJ(); j++)
                    max =  Math.Max(max, array[i, j]);
                result[i] = max;
            }

            return result;
        }

        /// <summary>
        /// Returns the maximum of each column.
        /// </summary>
        public double[] MaxPerJ()
        {
            var result = new double[array.LengthJ()];
            for (int j = 0; j < array.LengthJ(); j++)
            {
                var max = double.MinValue;
                for (int i = 0; i < array.LengthI(); i++)
                    max =  Math.Max(max, array[i, j]);
                result[j] = max;
            }

            return result;
        }

        public static double[,] operator +(double[,] arr, double[,] other)
        {
            var result = new double[arr.LengthI(), arr.LengthJ()];
            for (int i = 0; i < arr.LengthI(); i++)
            for (int j = 0; j < arr.LengthJ(); j++)
                result[i, j] = arr[i, j] + other[i, j];
            return result;
        }

        public static double[,] operator *(double[,] arr, double factor)
        {
            var result = new double[arr.LengthI(), arr.LengthJ()];
            for (int i = 0; i < arr.LengthI(); i++)
            for (int j = 0; j < arr.LengthJ(); j++)
                result[i, j] = arr[i, j] * factor;
            return result;
        }

        public static double[,] operator *(double factor, double[,] arr)
        {
            var result = new double[arr.LengthI(), arr.LengthJ()];
            for (int i = 0; i < arr.LengthI(); i++)
            for (int j = 0; j < arr.LengthJ(); j++)
                result[i, j] = arr[i, j] * factor;
            return result;
        }
    }
}