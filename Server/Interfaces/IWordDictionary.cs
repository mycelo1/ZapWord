using ZapWord.Server.Models;

namespace ZapWord.Server.Interfaces;

public interface IWordDictionary
{
    Task<List<DictionaryApiWord>?> WordFetch(string Word);
    void CacheClear();
}