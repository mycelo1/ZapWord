namespace ZapWord.Shared.Classes;

public class ThreadSafeRandom
{
    private static readonly Random _random_global = new Random();
    [ThreadStatic] private static Random? _random_local = null;

    public static int Next(int min, int max)
    {
        if (_random_local is null)
        {
            int seed;
            lock (_random_global)
            {
                seed = _random_global.Next();
            }
            _random_local = new Random(seed);
        }
        return _random_local.Next(min, max);
    }
}