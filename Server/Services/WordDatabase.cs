using System.IO.Compression;
using System.Text.RegularExpressions;

namespace ZapWord.Server.Services;

public class WordDatabase : IWordDatabase
{
    public List<string> WordList { get { return _WordList; } }
    public Dictionary<int, List<long>> WordJoin { get { return _WordJoin; } }
    public Dictionary<long, (string, string)> SemanticTable { get { return _SemanticTable; } }

    private static List<string> _WordList = new();
    private static Dictionary<int, List<long>> _WordJoin = new();
    private static Dictionary<long, (string, string)> _SemanticTable = new();
    private static long semantic_index = 0;

    public static async Task<WordDatabase> Init()
    {
        var diag = new System.Diagnostics.Stopwatch();
        diag.Start();
        await LoadFile("adjective", "Data/data.adj.gz");
        await LoadFile("adverb", "Data/data.adv.gz");
        await LoadFile("noun", "Data/data.noun.gz");
        await LoadFile("verb", "Data/data.verb.gz");
        Console.WriteLine($"WordDatabase.Init:{_WordList.Count()}/{_WordJoin.Count()}/{_SemanticTable.Count()}/{diag.ElapsedMilliseconds}");
        return new WordDatabase();
    }

    private static async Task LoadFile(string category, string path)
    {
        using var file_stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var gzip_stream = new GZipStream(file_stream, CompressionMode.Decompress);
        using var text_stream = new StreamReader(gzip_stream);

        string? line_text;
        while ((line_text = await text_stream.ReadLineAsync()) != null)
        {
            var line_member = line_text.Split('|', StringSplitOptions.RemoveEmptyEntries);
            if (line_member.Length == 2)
            {
                if (Regex.Match(line_member[0], @"^[0-9]{8}\s").Success)
                {
                    var matches = Regex.Matches(line_member[0], @"\b[a-z]{3,15}\b", RegexOptions.IgnoreCase);
                    if (matches.Count() > 0)
                    {
                        var description = line_member[1].Trim();
                        _SemanticTable.Add(++semantic_index, (category, description));
                    }
                    foreach (var match in matches)
                    {
                        var word = match.ToString()?.Trim();
                        if (word is not null)
                        {
                            var hash = word.GetHashCode();
                            if (_WordJoin.TryGetValue(hash, out var out_semantic_list))
                            {
                                out_semantic_list.Add(semantic_index);
                            }
                            else
                            {
                                List<long> new_semantic_list = new();
                                new_semantic_list.Add(semantic_index);
                                _WordList.Add(word);
                                _WordJoin.Add(hash, new_semantic_list);
                            }
                        }
                    }
                }
            }
        }
    }
}