namespace ZapWord.Server.Classes;

public class PermutationKN : List<int[]>
{
    private int K;
    private int N;
    private int maxPerm;
    private int[] current;

    public PermutationKN(int n, int k)
    {
        (K, N) = (k, n);
        current = Enumerable.Range(0, n).ToArray();
        Add(current[0..k]);
        maxPerm = Factorial(n) / Factorial(n - k);
        while (Count < maxPerm)
        {
            Add(Next()!);
        }
    }

    private int[]? Next()
    {
        var edge = K - 1;
        var j = K;
        while ((j < N) && (current[edge] >= current[j])) j++;
        if (j < N)
        {
            Swap(ref current, edge, j);
        }
        else
        {
            Reverse(ref current, K, N - 1);
            var i = edge - 1;
            while ((i >= 0) && (current[i] >= current[i + 1])) i--;
            if (i < 0) return null;
            j = N - 1;
            while ((j > i) && (current[i] >= current[j])) j--;
            Swap(ref current, i, j);
            Reverse(ref current, i + 1, N - 1);
        }
        return current[0..K];
    }

    private void Swap(ref int[] array, int i, int j)
    {
        (array[i], array[j]) = (array[j], array[i]);
    }

    private void Reverse(ref int[] array, int i, int j)
    {
        for ((int a, int b) = (i, j); a < b; a++, b--)
        {
            (array[a], array[b]) = (array[b], array[a]);
        }
    }

    private int Factorial(int n)
    {
        return n == 0 ? 1 : n * Factorial(n - 1);
    }
}