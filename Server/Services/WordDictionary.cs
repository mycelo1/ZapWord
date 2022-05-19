using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace ZapWord.Server.Services;

public class WordDictionary : IWordDictionary
{
    private readonly ConcurrentDictionary<string, DictionaryEntry> DictionaryCacheStorage = new();
    private readonly ILogger<IGameFabric> _logger;
    private readonly GameOptions _gameOptions;
    private readonly IHttpClientFactory _clientFactory;

    private class DictionaryEntry
    {
        public List<DictionaryApiWord> Definitions { get; }
        public int HitCount { get { return _hitCount; } }
        private int _hitCount;
        public DictionaryEntry(List<DictionaryApiWord> definitions)
        {
            Definitions = definitions;
        }
        public void AddHit()
        {
            Interlocked.Increment(ref _hitCount);
        }
    }

    public WordDictionary(ILogger<IGameFabric> logger, IOptions<GameOptions> options, IHttpClientFactory clientFactory)
    {
        _logger = logger;
        _gameOptions = options.Value;
        _clientFactory = clientFactory;
    }

    public async Task<List<DictionaryApiWord>?> WordFetch(string Word)
    {
        if (DictionaryCacheStorage.TryGetValue(Word, out var entry))
        {
            entry.AddHit();
            return entry.Definitions;
        }
        else
        {
            using var httpClient = _clientFactory.CreateClient();
            try
            {
                var response = await httpClient.GetFromJsonAsync<List<DictionaryApiWord>>(String.Format(_gameOptions.DictionaryApi, Word));
                DictionaryCacheStorage.TryAdd(Word, new DictionaryEntry(response!));
                return response!;
            }
            catch (HttpRequestException exception)
            {
                if (exception.StatusCode != System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning(exception, nameof(WordFetch));
                }
                return null;
            }
        }
    }

    public void CacheClear()
    {
        int count;
        if ((count = DictionaryCacheStorage.Count() - _gameOptions.MaxCachedWords) > 0)
        {
            _logger.LogWarning($"cleaning {count} entries off cache");
            foreach (var entry in DictionaryCacheStorage.OrderBy(e => e.Value.HitCount).Take(count))
            {
                DictionaryCacheStorage.TryRemove(entry.Key, out _);
            }
        }
    }
}