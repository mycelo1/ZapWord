using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using ZapWord.Shared.Classes;
using ZapWord.Shared.Models;

namespace ZapWord.Server.Services;

public class GameFabric : IGameFabric
{
    private readonly ILogger<IGameFabric> _logger;
    private readonly GameOptions _gameOptions;
    private readonly IWordDatabase _wordDatabase;
    private readonly IWordDictionary _wordDictionary;

    public ConcurrentQueue<ZapWordModel> GameQueue { get; } = new();

    public GameFabric(ILogger<IGameFabric> logger, IOptions<GameOptions> options, IWordDatabase wordDatabase, IWordDictionary wordDictionary)
    {
        _logger = logger;
        _gameOptions = options.Value;
        _wordDatabase = wordDatabase;
        _wordDictionary = wordDictionary;
    }

    public async Task<ZapWordModel> GameGet()
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

    public async Task GameEnqueue()
    {
        try
        {
            if (GameQueue.Count() < _gameOptions.MaxCachedGames)
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
            MinWordSize = _gameOptions.MinWordSize,
            MaxWordSize = _gameOptions.Letters
        };
        var letters = new List<char>();
        var word_list = new List<string>();
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("generating new game");
        do
        {
            for (var index = 0; index < _gameOptions.Letters; index++)
            {
                letters.Add(_gameOptions.LetterPool[ThreadSafeRandom.Next(0, _gameOptions.LetterPool.Length)]);
            }
            for (int length = _gameOptions.MinWordSize; length <= letters.Count(); length++)
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
                    if (_wordDatabase.WordList.BinarySearch(candidate_string) >= 0)
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
            foreach (var word in word_list.OrderBy(s => s.Length).ThenBy(s => s))
            {
                var semantics = new List<ZapWordModel.Semantic>();
                List<DictionaryApiWord>? dictionary;
                if ((dictionary = await _wordDictionary.WordFetch(word)) is not null)
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
            if (result.Words.Count() >= _gameOptions.MinWordCount)
            {
                break;
            }
            else
            {
                if (stopwatch.Elapsed > TimeSpan.FromSeconds(_gameOptions.GenerationTimeout))
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