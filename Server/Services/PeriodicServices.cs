using Microsoft.Extensions.Options;

namespace ZapWord.Server.Services;

public class PeriodicServices : IHostedService, IDisposable
{
    private Timer _timerGameFabricEnqueue = null!;
    private Timer _timerDictionaryCacheClear = null!;
    private readonly ILogger<PeriodicServices> _logger;
    private readonly GameOptions _gameOptions;
    private readonly IGameFabric _gameFabric;
    private readonly IWordDictionary _wordDictionary;

    public PeriodicServices(ILogger<PeriodicServices> logger, IOptions<GameOptions> options, IGameFabric gameFabric, IWordDictionary wordDictionary)
    {
        _logger = logger;
        _gameOptions = options.Value;
        _gameFabric = gameFabric;
        _wordDictionary = wordDictionary;
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        _timerGameFabricEnqueue = new Timer(async s => await GameFabricEnqueue(s), null, TimeSpan.Zero, TimeSpan.FromSeconds(_gameOptions.GenerationInterval));
        _timerDictionaryCacheClear = new Timer(DictionaryCacheClear, null, TimeSpan.Zero, TimeSpan.FromSeconds(_gameOptions.CacheCleaningInterval));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        _timerGameFabricEnqueue?.Change(Timeout.Infinite, 0);
        _timerDictionaryCacheClear?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timerGameFabricEnqueue?.Dispose();
        _timerDictionaryCacheClear?.Dispose();
    }

    private async Task GameFabricEnqueue(object? state)
    {
        try
        {
            _logger.LogInformation(nameof(GameFabricEnqueue));
            await _gameFabric.GameEnqueue();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, nameof(GameFabricEnqueue));
        }
    }

    private void DictionaryCacheClear(object? state)
    {
        try
        {
            _logger.LogInformation(nameof(DictionaryCacheClear));
            _wordDictionary.CacheClear();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, nameof(DictionaryCacheClear));
        }
    }
}