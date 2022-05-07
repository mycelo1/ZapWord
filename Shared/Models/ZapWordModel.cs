namespace ZapWord.Shared.Models;

public class ZapWordModel
{
    public int WordCount { get; set; }
    public List<char[]> Letters { get; set; } = new();
    public Dictionary<string, List<Semantic>> Words { get; set; } = new();
    public readonly record struct Semantic(string Category, string Description);
}