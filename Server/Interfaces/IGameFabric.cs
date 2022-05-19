using System.Collections.Concurrent;
using ZapWord.Shared.Models;

namespace ZapWord.Server.Interfaces;

public interface IGameFabric
{
    ConcurrentQueue<ZapWordModel> GameQueue { get; }
    Task<ZapWordModel> GameGet();
    Task GameEnqueue();
}
