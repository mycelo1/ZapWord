using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace ZapWord.Server.Interfaces;

public interface IWordDatabase
{
    ImmutableList<string> WordList { get; }
    ConcurrentDictionary<string, List<DictionaryApiWord>> DictionaryCache {get;}
}
