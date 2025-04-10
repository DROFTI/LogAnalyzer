using System.Collections;
using WPF.Models;

namespace WPF.Helpers
{
    public class LevenshteinComparer : IComparer
    {
        private readonly string _searchQuery;

        public LevenshteinComparer(string searchQuery)
        {
            _searchQuery = searchQuery;
        }

        public int Compare(object x, object y)
        {
            if (x is Log logX && y is Log logY)
            {
                int distanceX = ComputeLevenshteinDistance(logX.Message, _searchQuery);
                int distanceY = ComputeLevenshteinDistance(logY.Message, _searchQuery);

                int cmp = distanceX.CompareTo(distanceY);
                if (cmp == 0)
                {
                    return logX.Date.CompareTo(logY.Date);
                }
                return cmp;
            }
            return 0;
        }

        private int ComputeLevenshteinDistance(string source, string target)
        {
            if (string.IsNullOrEmpty(source))
                return target?.Length ?? 0;
            if (string.IsNullOrEmpty(target))
                return source.Length;

            int[,] matrix = new int[source.Length + 1, target.Length + 1];
            for (int i = 0; i <= source.Length; i++)
                matrix[i, 0] = i;
            for (int j = 0; j <= target.Length; j++)
                matrix[0, j] = j;

            for (int i = 1; i <= source.Length; i++)
            {
                for (int j = 1; j <= target.Length; j++)
                {
                    int cost = (source[i - 1] == target[j - 1]) ? 0 : 1;
                    matrix[i, j] = Math.Min(
                        Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                        matrix[i - 1, j - 1] + cost);
                }
            }
            return matrix[source.Length, target.Length];
        }
    }
}
