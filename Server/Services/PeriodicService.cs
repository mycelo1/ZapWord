using Microsoft.Extensions.Options;

namespace ZapWord.Server.Services;

public class PeriodicService : IHostedService, IDisposable
{
    private Timer _timer = null!;
    private readonly ILogger<PeriodicService> _logger;
    private readonly GameOptions _gameOptions;
    private readonly IGameFabric _gameFabric;

    public PeriodicService(ILogger<PeriodicService> logger, IOptions<GameOptions> options, IGameFabric gameFabric)
    {
        _logger = logger;
        _gameOptions = options.Value;
        _gameFabric = gameFabric;
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        _timer = new Timer(async s => await DoWork(s), null, TimeSpan.Zero, TimeSpan.FromSeconds(_gameOptions.GenerationInterval));
        return Task.CompletedTask;
    }

    private async Task DoWork(object? state)
    {
        try
        {
            await _gameFabric.EnqueueGame();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, nameof(DoWork));
        }
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}