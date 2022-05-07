namespace ZapWord.Server.Interfaces;

public interface IWordDatabase
{
    List<string> WordList { get; }
    Dictionary<int, List<long>> WordJoin { get;}
    Dictionary<long, (string, string)> SemanticTable { get;}
}
