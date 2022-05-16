using System.Collections.Concurrent;
using System.IO.Compression;
using ZapWord.Server.Models;

namespace ZapWord.Server.Services;

public class WordDatabase : IWordDatabase
{
    public HashSet<string> WordList { get { return _WordList; } }
    public ConcurrentDictionary<string, List<DictionaryApiWord>> DictionaryCache { get { return _DictionaryCache; } }

    private static HashSet<string> _WordList = new();
    private static ConcurrentDictionary<string, List<DictionaryApiWord>> _DictionaryCache = new();

    static WordDatabase()
    {
        LoadFile("Data/wordlist.txt.gz");
    }

    private static void LoadFile(string path)
    {
        using var file_stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var gzip_stream = new GZipStream(file_stream, CompressionMode.Decompress);
        using var text_stream = new StreamReader(gzip_stream);

        string? line_text;
        while ((line_text = text_stream.ReadLine()) != null)
        {
            _WordList.Add(line_text);
        }
    }
}