using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.IO.Compression;

namespace ZapWord.Server.Services;

public class WordDatabase : IWordDatabase
{
    public ImmutableList<string> WordList { get { return _WordList; } }
    public ConcurrentDictionary<string, List<DictionaryApiWord>> DictionaryCache { get { return _DictionaryCache; } }

    private static readonly ImmutableList<string> _WordList;
    private static readonly ConcurrentDictionary<string, List<DictionaryApiWord>> _DictionaryCache = new();

    static WordDatabase()
    {
        var word_list = LoadFile("Data/wordlist.txt.gz");
        _WordList = ImmutableList.CreateRange(word_list);
    }

    private static List<string> LoadFile(string path)
    {
        var word_list = new List<string>();
        using var file_stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var gzip_stream = new GZipStream(file_stream, CompressionMode.Decompress);
        using var text_stream = new StreamReader(gzip_stream);
        string? line_text;
        while ((line_text = text_stream.ReadLine()) != null)
        {
            word_list.Add(line_text);
        }
        word_list.Sort();
        return word_list;
    }
}