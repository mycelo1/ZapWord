using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using ZapWord.Shared.Models;
using ZapWord.Shared.Classes;

namespace ZapWord.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class ZapWordController : ControllerBase
{
    private const int LETTERS = 7;
    private const int MIN_WORD_SIZE = 3;
    private const int MIN_WORD_COUNT = 3;
    private const string ALPHABET = "eeeeeaaaaarrrrriiiiiooooottttnnnnssssllllccccuuudddpppmmmhhhggbbffyywwkkvxzjq";
    private readonly ILogger<ZapWordController> _logger;
    private readonly IWordDatabase _wordDatabase;

    public ZapWordController(ILogger<ZapWordController> logger, IWordDatabase wordDatabase)
    {
        _logger = logger;
        _wordDatabase = wordDatabase;
    }

    [HttpGet]
    public ZapWordModel Get()
    {
        return NewGame();
    }

    private ZapWordModel NewGame()
    {
        var result = new ZapWordModel
        {
            WordCount = _wordDatabase.WordList.Count(),
            MinWordSize = MIN_WORD_SIZE,
            MaxWordSize = LETTERS
        };
        var letters = new List<char>();
        var word_list = new List<string>();
        do
        {
            letters.Clear();
            word_list.Clear();
            result.Letters.Clear();
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
                    if (_wordDatabase.WordJoin.ContainsKey(candidate_string.GetHashCode()))
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
            if (word_list.Count() >= MIN_WORD_COUNT)
            {
                break;
            }
        }
        while (true);
        foreach (var word in word_list)
        {
            var semantics = new List<ZapWordModel.Semantic>();
            var hash_list = new HashSet<int>();
            foreach (var semantic_index in _wordDatabase.WordJoin[word.GetHashCode()])
            {
                var (category, description) = _wordDatabase.SemanticTable[semantic_index];
                var description_hash = description.GetHashCode();
                if (!hash_list.Contains(description_hash))
                {
                    description = Regex.Replace(description, @$"\b{word}\b", '\u2731'.ToString(), RegexOptions.IgnoreCase);
                    semantics.Add(new ZapWordModel.Semantic(category, description));
                    hash_list.Add(description_hash);
                }
            }
            result.Words.Add(word, semantics);
        }
        return result;
    }
}
