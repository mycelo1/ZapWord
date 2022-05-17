namespace ZapWord.Server.Models;

public class GameOptions
{
    public const string Section = "Game";
    public int Letters { get; set; }
    public int MinWordSize { get; set; }
    public int MinWordCount { get; set; }
    public int MaxCachedGames { get; set; }
    public int GenerationTimeout { get; set; }
    public int GenerationInterval { get; set; }
    public string LetterPool { get; set; } = String.Empty;
    public string DictionaryApi { get; set; } = String.Empty;
}