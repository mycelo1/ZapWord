using System.Collections.Concurrent;
using System.Diagnostics;
using ZapWord.Shared.Classes;
using ZapWord.Shared.Models;
using ZapWord.Server.Models;

namespace ZapWord.Server.Services;

public class GameFabric : IGameFabric
{
    private const int LETTERS = 7;
    private const int MIN_WORD_SIZE = 3;
    private const int MIN_WORD_COUNT = 3;
    private const int MAX_CACHED_GAMES = 10;
    private const int TIMEOUT_SECONDS = 60;
    private const string ALPHABET = "eeeeeaaaaarrrrriiiiiooooottttnnnnssssllllccccuuudddpppmmmhhhggbbffyywwkkvxzjq";

    private readonly ILogger<IGameFabric> _logger;
    private readonly IWordDatabase _wordDatabase;
    private readonly IHttpClientFactory _clientFactory;

    public ConcurrentQueue<ZapWordModel> GameQueue { get; } = new();

    public GameFabric(ILogger<IGameFabric> logger, IWordDatabase wordDatabase, IHttpClientFactory clientFactory)
    {
        _logger = logger;
        _wordDatabase = wordDatabase;
        _clientFactory = clientFactory;
    }

    public async Task<ZapWordModel> GetGame()
    {
        if (GameQueue.TryDequeue(out var result))
        {
            return result;
        }
        else
        {
            return await GenerateGame();
        }
    }

    public async Task EnqueueGame()
    {
        try
        {
            if (GameQueue.Count() < MAX_CACHED_GAMES)
            {
                GameQueue.Enqueue(await GenerateGame());
            }
        }
        catch (TimeoutException) { }
    }

    private async Task<ZapWordModel> GenerateGame()
    {
        var result = new ZapWordModel
        {
            WordCount = _wordDatabase.WordList.Count(),
            MinWordSize = MIN_WORD_SIZE,
            MaxWordSize = LETTERS
        };
        var letters = new List<char>();
        var word_list = new List<string>();
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("generating new game");
        do
        {
            for (var index = 0; index < LETTERS; index++)
            {
                letters.Add(ALPHABET[ThreadSafeRandom.Next(0, ALPHABET.Length)]);
            }
            for (int length = MIN_WORD_SIZE; length <= letters.Count(); length++)
            {
                var permutations = new PermutationKN(letters.Count(), length);
                foreach (var permutation in permutations)
                {
                    var candidate_chars = new List<char>();
                    foreach (var offset in permutation)
                    {
                        candidate_chars.Add(letters[offset]);
                    }
                    var candidate_chars_array = candidate_chars.ToArray();
                    var candidate_string = new String(candidate_chars_array);
                    if (_wordDatabase.WordList.Contains(candidate_string))
                    {
                        if (word_list.IndexOf(candidate_string) < 0)
                        {
                            word_list.Add(candidate_string);
                        }
                    }
                    if (length == letters.Count())
                    {
                        result.Letters.Add(candidate_chars_array);
                    }
                }
            }
            foreach (var word in word_list)
            {
                if (!_wordDatabase.DictionaryCache.ContainsKey(word))
                {
                    using var httpClient = _clientFactory.CreateClient();
                    try
                    {
                        var response = await httpClient.GetFromJsonAsync<List<DictionaryApiWord>>($"https://api.dictionaryapi.dev/api/v2/entries/en/{word}");
                        _wordDatabase.DictionaryCache.TryAdd(word, response!);
                    }
                    catch (HttpRequestException exception)
                    {
                        if (exception.StatusCode != System.Net.HttpStatusCode.NotFound)
                        {
                            _logger.LogWarning(exception, nameof(GenerateGame));
                        }
                    }
                }
            }
            foreach (var word in word_list.OrderBy(s => s.Length).ThenBy(s => s))
            {
                var semantics = new List<ZapWordModel.Semantic>();
                if (_wordDatabase.DictionaryCache.TryGetValue(word, out var dictionary))
                {
                    foreach (var variant in dictionary!)
                    {
                        if (variant.meanings is not null)
                        {
                            foreach (var meaning in variant.meanings!)
                            {
                                var category = meaning.partOfSpeech;
                                if (meaning.definitions is not null)
                                {
                                    foreach (var definition in meaning.definitions)
                                    {
                                        if ((category is not null) && (definition.definition is not null))
                                        {
                                            semantics.Add(new(category, definition.definition));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if (semantics.Count > 0)
                {
                    result.Words.Add(word, semantics);
                }
            }
            if (result.Words.Count() >= MIN_WORD_COUNT)
            {
                break;
            }
            else
            {
                if (stopwatch.Elapsed > TimeSpan.FromSeconds(TIMEOUT_SECONDS))
                {
                    _logger.LogWarning("took too long to generate game, giving up");
                    throw new TimeoutException();
                }
                else
                {
                    letters.Clear();
                    word_list.Clear();
                    result.Letters.Clear();
                    result.Words.Clear();
                }
            }
            _logger.LogInformation("didn't find enough words, trying again");
        }
        while (true);
        _logger.LogInformation($"game generated in {stopwatch.ElapsedMilliseconds} ms, {result.Words.Count} words found");
        return result;
    }
}