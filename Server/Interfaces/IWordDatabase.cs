using System.Collections.Concurrent;
using ZapWord.Server.Models;

namespace ZapWord.Server.Interfaces;

public interface IWordDatabase
{
    HashSet<string> WordList { get; }
    ConcurrentDictionary<string, List<DictionaryApiWord>> DictionaryCache {get;}
}
